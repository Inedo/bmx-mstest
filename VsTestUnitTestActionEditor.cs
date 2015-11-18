using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.IO;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.MsTest
{
    internal sealed class VsTestUnitTestActionEditor : ActionEditorBase
    {
        private ValidatingTextBox txtGroupName;
        private ValidatingTextBox txtContainer;
        private ValidatingTextBox txtAdditionalArguments;
        private CheckBox chkClearExistingTestResults;

        public override void BindToForm(ActionBase extension)
        {
            var vsTestExt = (VsTestUnitTestAction)extension;

            this.txtContainer.Text = PathEx.Combine(vsTestExt.OverriddenSourceDirectory, vsTestExt.TestContainer);
            this.txtAdditionalArguments.Text = vsTestExt.AdditionalArguments;
            this.txtGroupName.Text = vsTestExt.GroupName;
            this.chkClearExistingTestResults.Checked = vsTestExt.ClearExistingTestResults;
        }

        public override ActionBase CreateFromForm()
        {
            return new VsTestUnitTestAction()
            {
                TestContainer = PathEx.GetFileName(this.txtContainer.Text),
                OverriddenSourceDirectory = PathEx.GetDirectoryName(this.txtContainer.Text),
                AdditionalArguments = this.txtAdditionalArguments.Text,
                GroupName = this.txtGroupName.Text,
                ClearExistingTestResults = this.chkClearExistingTestResults.Checked
            };
        }

        protected override void CreateChildControls()
        {
            this.txtContainer = new ValidatingTextBox { Required = true };

            this.txtGroupName = new ValidatingTextBox { Required = true };

            this.txtAdditionalArguments = new ValidatingTextBox();

            this.chkClearExistingTestResults = new CheckBox { Text = "Clear existing TestResults directory" };

            this.Controls.Add(
                new SlimFormField("Test container:", this.txtContainer)
                {
                    HelpText = "The assembly that contains the tests to run, relative to the default directory."
                },
                new SlimFormField("Additional arguments:", this.txtAdditionalArguments)
                {
                    HelpText = "Optionally provide any additional arguments for the vstest.console executable."
                },
                new SlimFormField("Options:", this.chkClearExistingTestResults),
                new SlimFormField("Group name:", this.txtGroupName)
            );
        }
    }
}
