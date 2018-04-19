using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Inedo.Agents;
using Inedo.BuildMaster.Data;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.Operations;
using Inedo.IO;

namespace Inedo.BuildMasterExtensions.MsTest.Operations
{
    [ScriptAlias("Execute-VSTest")]
    [DisplayName("Execute VSTest Tests")]
    [Description("Runs VSTest unit tests on a specified test project, recommended for tests in VS 2012 and later.")]
    public sealed class VsTestOperation : ExecuteOperation
    {
        [Required]
        [ScriptAlias("TestContainer")]
        [DisplayName("Test container")]
        [Description("The file name of the test container assembly.")]
        public string TestContainer { get; set; }

        [Required]
        [ScriptAlias("Group")]
        [DisplayName("Test group")]
        [Description("The BuildMaster unit test group to record these tests in.")]
        public string TestGroup { get; set; }

        [ScriptAlias("Arguments")]
        [DisplayName("Additional arguments")]
        [Description("Additional arguments that will be passed to vstest.")]
        public string AdditionalArguments { get; set; }

        [ScriptAlias("ClearExistingTestResults")]
        [DisplayName("Clear existing results")]
        [Description("When true, the test results directory will be cleared before the tests are run.")]
        public bool ClearExistingTestResults { get; set; }

        [ScriptAlias("VsTestPath")]
        [DisplayName("VSTest Path")]
        [DefaultValue("$VSTestExePath")]
        [Description(@"The path to vstest.console.exe, typically: <br /><br />"
    + @"""C:\Program Files (x86)\Microsoft Visual Studio 11.0\Common7\IDE\CommonExtensions "
    + @"\Microsoft\TestWindow\vstest.console.exe""")]
        public string VsTestPath { get; set; }

        public override async Task ExecuteAsync(IOperationExecutionContext context)
        {
            var vsTestPath = this.GetVsTestPath(context);
            if (string.IsNullOrEmpty(vsTestPath))
                return;

            var fileOps = context.Agent.GetService<IFileOperationsExecuter>();

            var containerPath = context.ResolvePath(this.TestContainer);
            var sourceDirectory = PathEx.GetDirectoryName(containerPath);
            var resultsPath = PathEx.Combine(sourceDirectory, "TestResults");

            if (this.ClearExistingTestResults)
            {
                this.LogDebug($"Clearing {resultsPath} directory...");
                fileOps.ClearDirectory(resultsPath);
            }

            await this.ExecuteCommandLineAsync(
                context,
                new RemoteProcessStartInfo
                {
                    FileName = vsTestPath,
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

            using (var db = new DB.Context())
            {
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

                    await db.BuildTestResults_RecordTestResultAsync(
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
        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            return new ExtendedRichDescription(
                new RichDescription(
                    "Run VSTest on ",
                    new DirectoryHilite(config[nameof(this.TestContainer)])
                )
            );
        }

        private string GetVsTestPath(IOperationExecutionContext context)
        {
            if (string.IsNullOrWhiteSpace(this.VsTestPath))
            {
                this.LogDebug("$VSTestExePath variable is not defined; attempting to locate vstest.console.exe...");
                var vsTestPath = context.Agent.GetService<IRemoteMethodExecuter>().InvokeFunc(FindVsTestExe);
                if (string.IsNullOrEmpty(vsTestPath))
                {
                    this.LogError("Unable to find vstest.console.exe. Verify that VSTest is installed and set a $VSTestExePath server variable to its full path.");
                    return null;
                }

                return vsTestPath;
            }
            else
            {
                this.LogDebug("VSTestExePath = " + this.VsTestPath);
                if (!context.Agent.GetService<IFileOperationsExecuter>().FileExists(this.VsTestPath))
                {
                    this.LogError($"The file {this.VsTestPath} does not exist. Verify that VSTest is installed.");
                    return null;
                }

                return this.VsTestPath;
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

        private static string FindVsTestExe()
        {
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            var visualStudioDirs = (from d in Directory.EnumerateDirectories(programFiles)
                                    let m = Regex.Match(d, @"\\Microsoft Visual Studio (?<1>[0-9]+(\.[0-9]+)?)$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture)
                                    where m.Success
                                    orderby decimal.Parse(m.Groups[1].Value) descending
                                    select d);

            foreach (var vsDir in visualStudioDirs)
            {
                var vsTestExePath = Path.Combine(vsDir, "Common7", "IDE", "CommonExtensions", "Microsoft", "TestWindow", "vstest.console.exe");
                if (File.Exists(vsTestExePath))
                    return vsTestExePath;
            }

            return null;
        }
    }
}
