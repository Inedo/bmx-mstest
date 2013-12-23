using System.IO;
using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.MsTest
{
    /// <summary>
    /// Custom editor for <see cref="VsTestUnitTestAction"/>
    /// </summary>
    public sealed class VsTestUnitTestActionEditor : ActionEditorBase
    {
        public VsTestUnitTestActionEditor()
        {
        }

        private ValidatingTextBox txtGroupName;
        private ValidatingTextBox txtContainer;
        private ValidatingTextBox txtAdditionalArguments;
        private CheckBox chkClearExistingTestResults;

        /// <summary>
        /// Binds to form.
        /// </summary>
        /// <param name="extension">The extension.</param>
        public override void BindToForm(ActionBase extension)
        {
            var vsTestExt = (VsTestUnitTestAction)extension;

            this.txtContainer.Text = Path.Combine(vsTestExt.OverriddenSourceDirectory, vsTestExt.TestContainer);
            this.txtAdditionalArguments.Text = vsTestExt.AdditionalArguments;
            this.txtGroupName.Text = vsTestExt.GroupName;
            this.chkClearExistingTestResults.Checked = vsTestExt.ClearExistingTestResults;
        }

        /// <summary>
        /// Creates from form.
        /// </summary>
        /// <returns></returns>
        public override ActionBase CreateFromForm()
        {
            return new VsTestUnitTestAction()
            {
                TestContainer = Path.GetFileName(this.txtContainer.Text),
                OverriddenSourceDirectory = Path.GetDirectoryName(this.txtContainer.Text),
                AdditionalArguments = this.txtAdditionalArguments.Text,
                GroupName = this.txtGroupName.Text,
                ClearExistingTestResults = this.chkClearExistingTestResults.Checked
            };
        }

        protected override void CreateChildControls()
        {
            this.txtContainer = new ValidatingTextBox()
            {
                Width = 300,
                Required = true
            };

            this.txtGroupName = new ValidatingTextBox() { Width = 300, Required = true };

            this.txtAdditionalArguments = new ValidatingTextBox() { Width = 300 };

            this.chkClearExistingTestResults = new CheckBox() { Text = "Clear existing TestResults directory" };

            this.Controls.Add(
                 new FormFieldGroup("Test File/Container",
                     "The assembly that contains the tests to run, relative to the default directory.",
                     false,
                     new StandardFormField("Test Container:", this.txtContainer)
                 ),
                 new FormFieldGroup("Additional Arguments",
                     "Optionally provide any additional arguments for the vstest.console executable.",
                     false,
                     new StandardFormField("Additional Arguments:", this.txtAdditionalArguments)
                 ),
                 new FormFieldGroup("Options",
                     "Optionally delete all files in the TestResults directory if it exists before running the unit tests.",
                     false,
                     new StandardFormField("", this.chkClearExistingTestResults)
                 ),
                 new FormFieldGroup("Group Name",
                     "The Group Name allows you to easily identify the unit tests.",
                     true,
                     new StandardFormField("Group Name:", this.txtGroupName)
                 )
            );
        }
    }
}
