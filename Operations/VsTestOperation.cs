using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Inedo.BuildMaster.Data;
using Inedo.BuildMaster.Documentation;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Agents;
using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.Diagnostics;
using Inedo.IO;

namespace Inedo.BuildMasterExtensions.MsTest.Operations
{
    [ScriptAlias("Execute-VSTest")]
    [DisplayName("Execute VSTest Tests")]
    [Description("Runs VSTest unit tests on a specified test project, recommended for tests in VS 2012 and later.")]
    public sealed class VsTestOperation : ExecuteOperation
    {
        [ScriptAlias("TestContainer")]
        public string TestContainer { get; set; }

        [ScriptAlias("Arguments")]
        public string AdditionalArguments { get; set; }

        [ScriptAlias("ClearExistingTestResults")]
        public bool ClearExistingTestResults { get; set; }

        [ScriptAlias("Group")]
        public string TestGroup { get; set; }

        [ScriptAlias("VsTestPath")]
        [DisplayName("VSTest Path")]
        [Description(@"The path to vstest.console.exe, typically: <br /><br />"
    + @"""C:\Program Files (x86)\Microsoft Visual Studio 11.0\Common7\IDE\CommonExtensions "
    + @"\Microsoft\TestWindow\vstest.console.exe""")]
        public string VsTestPath { get; set; }

        public override async Task ExecuteAsync(IOperationExecutionContext context)
        {
            var fileOps = context.Agent.GetService<IFileOperationsExecuter>();
            if (!fileOps.FileExists(this.VsTestPath))
            {
                this.LogError("Could not find the file vstest.console.exe; ensure that the MsTest extensions's configuration is correct before continuing.");
                return;
            }

            var containerPath = context.ResolvePath(this.TestContainer);
            var sourceDirectory = PathEx.GetDirectoryName(containerPath);
            var resultsPath = PathEx.Combine(sourceDirectory, "TestResults");

            if (this.ClearExistingTestResults)
            {
                this.LogDebug($"Clearing {resultsPath} directory...");
                fileOps.ClearFolder(resultsPath);
            }

            await this.ExecuteCommandLineAsync(
                context,
                new AgentProcessStartInfo
                {
                    FileName = this.VsTestPath,
                    Arguments = $"\"{this.TestContainer}\" /logger:trx {this.AdditionalArguments}",
                    WorkingDirectory = sourceDirectory
                }
            );

            if (!fileOps.DirectoryExists(resultsPath))
            {
                this.LogError("Could not find the generated \"TestResults\" directory after running unit tests at: " + sourceDirectory);
                return;
            }

            var trxFiles = fileOps.GetFileSystemInfos(resultsPath, new MaskingContext(new[] { "*.trx" }, Enumerable.Empty<string>()))
                .OfType<SlimFileInfo>()
                .ToList();

            if (trxFiles.Count == 0)
            {
                this.LogError("There are no .trx files in the \"TestResults\" directory.");
                return;
            }

            var trxPath = trxFiles
                .Aggregate((latest, next) => next.LastWriteTimeUtc > latest.LastWriteTimeUtc ? next : latest)
                .FullName;

            XDocument doc;
            using (var file = fileOps.OpenFile(trxPath, FileMode.Open, FileAccess.Read))
            using (var reader = new XmlTextReader(file) { Namespaces = false })
            {
                doc = XDocument.Load(reader);
            }

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

                DB.BuildTestResults_RecordTestResult(
                    Execution_Id: context.ExecutionId,
                    Group_Name: this.TestGroup,
                    Test_Name: testName,
                    TestStatus_Code: testStatusCode,
                    TestResult_Text: testResult,
                    TestStarted_Date: startDate,
                    TestEnded_Date: endDate
                );
            }
        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            return new ExtendedRichDescription(
                new RichDescription(
                    "Run VSTest on ",
                    new DirectoryHilite(this.TestContainer)
                )
            );
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
