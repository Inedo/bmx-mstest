using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Data;
using Inedo.BuildMaster.Documentation;
using Inedo.BuildMaster.Extensibility.Actions.Testing;
using Inedo.BuildMaster.Extensibility.Agents;
using Inedo.BuildMaster.Files;
using Inedo.BuildMaster.Web;
using Inedo.Serialization;

namespace Inedo.BuildMasterExtensions.MsTest
{
    [DisplayName("Execute VSTest Tests")]
    [Description("Runs VSTest unit tests on a specified test project, recommended for tests in VS 2012 and later.")]
    [Tag(Tags.UnitTests)]
    [CustomEditor(typeof(VsTestUnitTestActionEditor))]
    [RequiresInterface(typeof(IFileOperationsExecuter))]
    public sealed class VsTestUnitTestAction : UnitTestActionBase
    {
        [Persistent]
        public string TestContainer { get; set; }

        [Persistent]
        public string AdditionalArguments { get; set; }

        [Persistent]
        public bool ClearExistingTestResults { get; set; }

        public override ExtendedRichDescription GetActionDescription()
        {
            return new ExtendedRichDescription(
                new RichDescription(
                    "Run VSTest tests on ",
                    new Hilite(this.TestContainer)
                ),
                new RichDescription(
                    "in ",
                    new DirectoryHilite(this.OverriddenSourceDirectory)
                )
            );
        }

        protected override void RunTests()
        {
            var configurer = (MsTestConfigurer)this.GetExtensionConfigurer();
            if (configurer == null || string.IsNullOrEmpty(configurer.VsTestPath))
                throw new InvalidOperationException("To run VSTests, the path to vstest.console.exe must be set in the MsTest extension's configuration.");

            var fileOps = this.Context.Agent.GetService<IFileOperationsExecuter>();
            if (!fileOps.FileExists(configurer.VsTestPath))
                throw new FileNotFoundException("Could not find the file vstest.console.exe; ensure that the MsTest extensions's configuration is correct before continuing.", configurer.VsTestPath);

            string resultsPath = fileOps.CombinePath(this.Context.SourceDirectory, "TestResults");

            if (this.ClearExistingTestResults)
            {
                this.LogDebug($"Clearing {resultsPath} directory...");
                fileOps.ClearFolder(resultsPath);
            }

            this.ExecuteCommandLine(
                configurer.VsTestPath,
                $"{this.TestContainer} /logger:trx {this.AdditionalArguments}",
                this.Context.SourceDirectory
            );

            if (!fileOps.DirectoryExists(resultsPath))
                throw new DirectoryNotFoundException("Could not find the generated \"TestResults\" directory after running unit tests at: " + resultsPath);

            var resultsDir = fileOps.GetDirectoryEntry(
                new GetDirectoryEntryCommand()
                {
                    Path = resultsPath,
                    Recurse = false,
                    IncludeRootPath = true
                }
            ).Entry;

            var trxFiles = resultsDir.Files.Where(f => f.Name.EndsWith(".trx")).Cast<ExtendedFileEntryInfo>().ToList();
            if (!trxFiles.Any())
                throw new InvalidOperationException("There are no .trx files in the \"TestResults\" directory.");

            string trxPath = trxFiles
                .Aggregate((latest, next) => next.LastModifiedDate > latest.LastModifiedDate ? next : latest)
                .Path;

            using (var file = fileOps.OpenFile(trxPath, FileMode.Open, FileAccess.Read))
            using (var reader = new XmlTextReader(file) { Namespaces = false })
            {
                var doc = XDocument.Load(reader);
                foreach (var result in doc.Element("TestRun").Element("Results").Elements("UnitTestResult"))
                {
                    var testName = (string)result.Attribute("testName");
                    var outcome = (string)result.Attribute("outcome");
                    var output = result.Element("Output");
                    string testStatusCode;
                    string testResult;

                    if (string.Equals(outcome, "Passed", StringComparison.OrdinalIgnoreCase))
                    {
                        testStatusCode = Domains.TestStatusCodes.Passed;
                        testResult = "Passed";
                    }
                    else if (string.Equals(outcome, "NotExecuted", StringComparison.OrdinalIgnoreCase))
                    {
                        testStatusCode = Domains.TestStatusCodes.Inconclusive;
                        if (output == null)
                            testResult = "Ignored";
                        else
                            testResult = GetResultTextFromOutput(output);
                    }
                    else
                    {
                        testStatusCode = Domains.TestStatusCodes.Failed;
                        testResult = GetResultTextFromOutput(output);
                    }

                    var startDate = (DateTime)result.Attribute("startTime");
                    var endDate = (DateTime)result.Attribute("endTime");

                    this.RecordResult(testName, testStatusCode, testResult, startDate, endDate);
                }
            }
        }

        private static string GetResultTextFromOutput(XElement output)
        {
            var message = string.Empty;
            var errorInfo = output.Element("ErrorInfo");
            if (errorInfo != null)
            {
                message = (string)errorInfo.Element("Message");
                var trace = (string)errorInfo.Element("StackTrace");
                if (!string.IsNullOrEmpty(trace))
                    message += Environment.NewLine + trace;
            }

            return message;
        }
    }
}
