using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.Amazon.EC2
{
    /// <summary>
    /// Custom editor for the Terminate EC2 instance action.
    /// </summary>
    internal sealed class TerminateEC2InstanceActionEditor : ActionEditorBase
    {
        private ValidatingTextBox txtInstanceId;

        /// <summary>
        /// Initializes a new instance of the <see cref="TerminateEC2InstanceActionEditor"/> class.
        /// </summary>
        public TerminateEC2InstanceActionEditor()
        {
        }

        public override void BindToForm(ActionBase extension)
        {
            EnsureChildControls();

            var terminateInstanceAction = (TerminateEC2InstanceAction)extension;
            txtInstanceId.Text = terminateInstanceAction.InstanceIdOrIPAddress;
        }
        public override ActionBase CreateFromForm()
        {
            EnsureChildControls();

            return new TerminateEC2InstanceAction()
            {
                InstanceIdOrIPAddress = txtInstanceId.Text
            };
        }

        /// <summary>
        /// Called by the ASP.NET page framework to notify server controls that use composition-based
        /// implementation to create any child controls they contain in preparation for posting back or rendering.
        /// </summary>
        protected override void CreateChildControls()
        {
            base.CreateChildControls();

            txtInstanceId = new ValidatingTextBox()
            {
                Width = Unit.Pixel(300),
                Required = true
            };

            CUtil.Add(this,
                new FormFieldGroup(
                    "Instance",
                    "Enter the ID or public IP of the EC2 instance to terminate.",
                    false,
                    new StandardFormField("Instance:", txtInstanceId)
                )
            );
        }
    }
}
