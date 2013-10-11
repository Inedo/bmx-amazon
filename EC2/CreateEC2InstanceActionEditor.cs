using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.Amazon.EC2
{
    /// <summary>
    /// Custom editor for the create EC2 instance action.
    /// </summary>
    internal sealed class CreateEC2InstanceActionEditor : ActionEditorBase
    {
        private ValidatingTextBox txtAmiID;
        private ValidatingTextBox txtIPAddress;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateEC2InstanceActionEditor"/> class.
        /// </summary>
        public CreateEC2InstanceActionEditor()
        {
            ValidateBeforeSave += CreateAmazonEc2InstanceActionEditor_ValidateBeforeSave;
        }

        public override void BindToForm(ActionBase extension)
        {
            EnsureChildControls();

            var runInstanceAction = (CreateEC2InstanceAction)extension;
            txtAmiID.Text = runInstanceAction.AmiID;
        }
        public override ActionBase CreateFromForm()
        {
            EnsureChildControls();

            return new CreateEC2InstanceAction()
            {
                AmiID = txtAmiID.Text
            };
        }

        /// <summary>
        /// Called by the ASP.NET page framework to notify server controls that use composition-based
        /// implementation to create any child controls they contain in preparation for posting back or rendering.
        /// </summary>
        protected override void CreateChildControls()
        {
            base.CreateChildControls();

            txtAmiID = new ValidatingTextBox()
            {
                Width = Unit.Pixel(300),
                Required = true
            };

            txtIPAddress = new ValidatingTextBox()
            {
                Width = Unit.Pixel(300)
            };

            CUtil.Add(this,
                new FormFieldGroup(
                    "Amazon Image ID",
                    "Specify an image ID you would like to use to create the instance.",
                    false,
                    new StandardFormField("Image ID:", txtAmiID)),
                new FormFieldGroup(
                    "IP Address",
                    "Specify an optional public IP address to associate with this instance.",
                    true,
                    new StandardFormField("IP Address:", txtIPAddress))
                );
        }

        /// <summary>
        /// Handles the ValidateBeforeSave event of the CreateAmazonEc2InstanceActionEditor control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Inedo.BuildMaster.Web.Controls.Extensions.ValidationEventArgs&lt;Inedo.BuildMaster.Extensibility.Actions.ActionBase&gt;"/> instance containing the event data.</param>
        private void CreateAmazonEc2InstanceActionEditor_ValidateBeforeSave(object sender, ValidationEventArgs<ActionBase> e)
        {
            System.Net.IPAddress address;
            if (!string.IsNullOrEmpty(this.txtIPAddress.Text) && !System.Net.IPAddress.TryParse(this.txtIPAddress.Text, out address))
            {
                e.Message = "IP address is not valid.";
                e.ValidLevel = ValidationLevel.Error;
            }
        }
    }
}
