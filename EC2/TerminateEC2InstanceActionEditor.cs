using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.Amazon.EC2
{
    internal sealed class TerminateEC2InstanceActionEditor : ActionEditorBase
    {
        private ValidatingTextBox txtInstanceId;

        public override void BindToForm(ActionBase extension)
        {
            var terminateInstanceAction = (TerminateEC2InstanceAction)extension;
            this.txtInstanceId.Text = terminateInstanceAction.InstanceIdOrIPAddress;
        }
        public override ActionBase CreateFromForm()
        {
            return new TerminateEC2InstanceAction
            {
                InstanceIdOrIPAddress = this.txtInstanceId.Text
            };
        }

        protected override void CreateChildControls()
        {
            this.txtInstanceId = new ValidatingTextBox { Required = true };

            this.Controls.Add(
                new SlimFormField("Instance:", this.txtInstanceId)
            );
        }
    }
}
