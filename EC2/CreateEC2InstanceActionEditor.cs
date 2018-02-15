using System.Net;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.Amazon.EC2
{
    internal sealed class CreateEC2InstanceActionEditor : ActionEditorBase
    {
        private ValidatingTextBox txtAmiID;
        private ValidatingTextBox txtIPAddress;

        public CreateEC2InstanceActionEditor()
        {
            this.ValidateBeforeSave += this.CreateAmazonEc2InstanceActionEditor_ValidateBeforeSave;
        }

        public override void BindToForm(ActionBase extension)
        {
            var runInstanceAction = (CreateEC2InstanceAction)extension;
            this.txtAmiID.Text = runInstanceAction.AmiID;
        }
        public override ActionBase CreateFromForm()
        {
            return new CreateEC2InstanceAction
            {
                AmiID = this.txtAmiID.Text
            };
        }

        protected override void CreateChildControls()
        {
            this.txtAmiID = new ValidatingTextBox { Required = true };
            this.txtIPAddress = new ValidatingTextBox { DefaultText = "No public IP" };

            this.Controls.Add(
                new SlimFormField("Amazon image ID:", this.txtAmiID),
                new SlimFormField("IP address:", this.txtIPAddress)
            );
        }

        private void CreateAmazonEc2InstanceActionEditor_ValidateBeforeSave(object sender, ValidationEventArgs<ActionBase> e)
        {
            IPAddress address;
            if (!string.IsNullOrEmpty(this.txtIPAddress.Text) && !IPAddress.TryParse(this.txtIPAddress.Text, out address))
            {
                e.Message = "IP address is not valid.";
                e.ValidLevel = ValidationLevel.Error;
            }
        }
    }
}
