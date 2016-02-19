using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.Amazon.CloudFormation
{
    internal sealed class WaitForStackCreationActionEditor : ActionEditorBase
    {
        private ValidatingTextBox txtStackName;

        public override void BindToForm(ActionBase extension)
        {
            var action = (WaitForStackCreationAction)extension;
            this.txtStackName.Text = action.StackName;
        }

        public override ActionBase CreateFromForm()
        {
            return new WaitForStackCreationAction
            {
                StackName = this.txtStackName.Text
            };
        }

        protected override void CreateChildControls()
        {
            this.txtStackName = new ValidatingTextBox { Required = true };

            this.Controls.Add(
                new SlimFormField("Stack name:", this.txtStackName)
            );
        }
    }
}
