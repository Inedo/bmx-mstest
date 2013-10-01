using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.MsTest
{
    internal sealed class MsTestUnitTestActionEditor : ActionEditorBase
    {
        public override bool DisplaySourceDirectory { get { return true; } }

        private SourceControlFileFolderPicker txtExecutablePath;
        private ValidatingTextBox txtTestFile, txtGroupName, txtTestSettingsFilePath, txtAdditionalArguments;

        protected override void CreateChildControls()
        {
            this.txtExecutablePath = new SourceControlFileFolderPicker()
            {
                ServerId = this.ServerId,
                DisplayMode = SourceControlBrowser.DisplayModes.FoldersAndFiles
            };

            this.txtTestFile = new ValidatingTextBox()
            {
                Width = 300,
                Required = true
            };

            this.txtGroupName = new ValidatingTextBox() { Width = 300 };

            this.txtTestSettingsFilePath = new ValidatingTextBox() { Width = 300 };
            
            this.txtAdditionalArguments = new ValidatingTextBox() { Width = 300 };

            CUtil.Add(this,
                 new FormFieldGroup("MSTest Executable Path",
                    "The path of the MSTest executable on the remote server.",
                    false,
                    new StandardFormField("MSTest Executable:", txtExecutablePath)
                 ),
                 new FormFieldGroup("Test File",
                     "The DLL, assembly, or project file to test against.",
                     false,
                     new StandardFormField("Test File:", txtTestFile)
                 ),
                 new FormFieldGroup("Settings File Path",
                     "Specify an alternate test settings file to use relative to the working directory, or leave blank to use the default.",
                     false,
                     new StandardFormField("Settings File Path:", txtTestSettingsFilePath)
                 ),
                 new FormFieldGroup("Additional Arguments",
                     "If there are any additional arguments for the MSTest executable, add them here.",
                     false,
                     new StandardFormField("Additional Arguments:", txtAdditionalArguments)    
                 ),
                 new FormFieldGroup("Group Name",
                     "The Group Name allows you to easily identify the unit test.",
                     true,
                     new StandardFormField("Group Name:", txtGroupName)
                 )
            );

            base.CreateChildControls();
        }

        public override void BindToForm(ActionBase extension)
        {
            EnsureChildControls();

            var msTestAction = (MsTestUnitTestAction)extension;

            txtExecutablePath.Text = msTestAction.MsTestExecutablePath;
            txtTestFile.Text = msTestAction.TestFile;
            txtGroupName.Text = msTestAction.GroupName;
            txtTestSettingsFilePath.Text = msTestAction.TestSettingsFilePath;
            txtAdditionalArguments.Text = msTestAction.AdditionalArguments;
        }

        public override ActionBase CreateFromForm()
        {
            EnsureChildControls();

            var msTestAction = new MsTestUnitTestAction();

            msTestAction.MsTestExecutablePath = txtExecutablePath.Text;
            msTestAction.TestFile = txtTestFile.Text;
            msTestAction.GroupName = txtGroupName.Text;
            msTestAction.TestSettingsFilePath = txtTestSettingsFilePath.Text;
            msTestAction.AdditionalArguments = txtAdditionalArguments.Text;

            return msTestAction;
        }
    }
}
