using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.MsTest
{
    internal sealed class MsTestUnitTestActionEditor : ActionEditorBase
    {
        public override bool DisplaySourceDirectory => true;

        private SourceControlFileFolderPicker txtExecutablePath;
        private ValidatingTextBox txtTestFile, txtGroupName, txtTestSettingsFilePath, txtAdditionalArguments;

        protected override void CreateChildControls()
        {
            this.txtExecutablePath = new SourceControlFileFolderPicker
            {
                ServerId = this.ServerId,
                DisplayMode = SourceControlBrowser.DisplayModes.FoldersAndFiles
            };

            this.txtTestFile = new ValidatingTextBox
            {
                Required = true
            };

            this.txtGroupName = new ValidatingTextBox();

            this.txtTestSettingsFilePath = new ValidatingTextBox();

            this.txtAdditionalArguments = new ValidatingTextBox();

            this.Controls.Add(
                new SlimFormField("MSTest executable:", txtExecutablePath),
                new SlimFormField("Test file:", txtTestFile),
                new SlimFormField("Settings file path:", txtTestSettingsFilePath),
                new SlimFormField("Additional arguments:", txtAdditionalArguments),
                new SlimFormField("Group name:", txtGroupName)
            );
        }

        public override void BindToForm(ActionBase extension)
        {
            var msTestAction = (MsTestUnitTestAction)extension;

            txtExecutablePath.Text = msTestAction.MsTestExecutablePath;
            txtTestFile.Text = msTestAction.TestFile;
            txtGroupName.Text = msTestAction.GroupName;
            txtTestSettingsFilePath.Text = msTestAction.TestSettingsFilePath;
            txtAdditionalArguments.Text = msTestAction.AdditionalArguments;
        }

        public override ActionBase CreateFromForm()
        {
            return new MsTestUnitTestAction
            {
                MsTestExecutablePath = txtExecutablePath.Text,
                TestFile = txtTestFile.Text,
                GroupName = txtGroupName.Text,
                TestSettingsFilePath = txtTestSettingsFilePath.Text,
                AdditionalArguments = txtAdditionalArguments.Text
            };
        }
    }
}
