using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Xml;
using Inedo.BuildMaster.Extensibility.Actions.Testing;
using Inedo.Serialization;
using Inedo.Web;

namespace Inedo.BuildMasterExtensions.MsTest
{
    [DisplayName("Execute MSTest Tests")]
    [Description("Runs MSTest unit tests on a specified test project, recommended for tests in VS 2010 and earlier.")]
    [CustomEditor(typeof(MsTestUnitTestActionEditor))]
    [PersistFrom("Inedo.BuildMasterExtensions.MsTest.MsTestUnitTestAction,MsTest")]
    public sealed class MsTestUnitTestAction : UnitTestActionBase
    {
        private const string TestResultsFile = "mstestresults.xml";

        [Persistent]
        public string TestFile { get; set; }

        [Persistent]
        public string MsTestExecutablePath { get; set; }

        [Persistent]
        public string TestSettingsFilePath { get; set; }

        [Persistent]
        public string AdditionalArguments { get; set; }

        private static class MsTestStatuses
        {
            public const string Passed = "Passed";
            public const string Failed = "Failed";
            public const string Inconclusive = "Inconclusive";
        }

        protected override void RunTests()
        {
            var args = BuildArguments();
            string exitCode = ExecuteCommandLine(
                this.MsTestExecutablePath,
                args,
                this.Context.SourceDirectory
            ).ToString();

            var xmlDoc = new XmlDocument();

            //jr:Ignore/jr:XML/jr:namespaces
            using (XmlTextReader tr = new XmlTextReader(Path.Combine(this.Context.SourceDirectory, TestResultsFile)))
            {
                tr.Namespaces = false;
                xmlDoc.Load(tr);
            }

            foreach (XmlElement resultNode in xmlDoc.SelectNodes("/TestRun/Results//UnitTestResult"))
            {
                var testName = resultNode.Attributes["testName"].Value;
                var testStatus = resultNode.Attributes["outcome"].Value;
                var testResult = string.Empty;
                if (testStatus != MsTestStatuses.Passed)
                {
                    var messageNode = resultNode.SelectSingleNode("Output/ErrorInfo/Message");
                    if (messageNode != null)
                        testResult = messageNode.InnerText;
                }

                // skip tests that were inconclusive
                if (testStatus == MsTestStatuses.Inconclusive)
                {
                    this.LogInformation(string.Format("MSTest Test: {0} ({1})", testName, testStatus));
                    continue;
                }

                var durationAttribute = resultNode.GetAttribute("duration");
                if (!string.IsNullOrEmpty(durationAttribute))
                {
                    this.LogInformation(string.Format("MSTest Test: {0}, Result: {1}, Test Length: {2} secs",
                        testName,
                        testStatus,
                        TimeSpan.Parse(durationAttribute).TotalSeconds)
                    );
                }
                else
                {
                    this.LogInformation(string.Format("MSTest Test: {0}, Result: {1}",
                        testName,
                        testStatus)
                    );
                }

                var startTimeAttribute = resultNode.GetAttribute("startTime");
                var endTimeAttribute = resultNode.GetAttribute("endTime");

                var startTime = !string.IsNullOrEmpty(startTimeAttribute) ? DateTime.Parse(startTimeAttribute) : DateTime.Now;
                var endTime = !string.IsNullOrEmpty(endTimeAttribute) ? DateTime.Parse(endTimeAttribute) : startTime;

                this.RecordResult(
                    testName,
                    testStatus == MsTestStatuses.Passed,
                    testResult,
                    startTime,
                    endTime
                );
            }
        }

        private string BuildArguments()
        {
            var args = new List<string>();

            // saves the results XML file to the source directory
            args.Add(String.Format("/resultsfile:\"{0}\"", Path.Combine(this.Context.SourceDirectory, TestResultsFile)));

            // don't show copyright info & logo
            args.Add("/nologo");

            // specify which assembly to test
            args.Add(string.Format("/testcontainer:\"{0}\"", this.TestFile));

            // if there is a settings file relative to the source directory, use it
            if (!string.IsNullOrEmpty(this.TestSettingsFilePath))
                args.Add(string.Format("/testsettings:\"{0}\"", Path.Combine(this.Context.SourceDirectory, this.TestSettingsFilePath)));

            // add any additional arguments
            args.Add(this.AdditionalArguments);

            return string.Join(" ", args.ToArray());
        }

        public override string ToString() => "Run MSTest tests on " + this.TestFile;
    }
}
