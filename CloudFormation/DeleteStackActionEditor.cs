using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.Amazon.CloudFormation
{
    internal sealed class DeleteStackActionEditor : ActionEditorBase 
    {
        private ValidatingTextBox txtStackName;
        private CheckBox chkWaitUntilComplete;

        public override void BindToForm(ActionBase extension)
        {
            var action = (DeleteStackAction)extension;
            this.txtStackName.Text = action.StackName;
            this.chkWaitUntilComplete.Checked = action.WaitUntilComplete;
        }

        public override ActionBase CreateFromForm()
        {
            return new DeleteStackAction
            {
                StackName = this.txtStackName.Text,
                WaitUntilComplete = this.chkWaitUntilComplete.Checked
            };
        }

        protected override void CreateChildControls()
        {
            this.txtStackName = new ValidatingTextBox { Required = true };
            this.chkWaitUntilComplete = new CheckBox { Text = "Wait until complete" };

            this.Controls.Add(
                new SlimFormField("Stack name:", this.txtStackName),
                new SlimFormField("Options:", this.chkWaitUntilComplete)
            );
        }
    }
}
