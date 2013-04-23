using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls;
using Inedo.Web.Controls;

namespace CloudFormation.Actions
{
    public class WaitForStackCreationActionEditor : CloudFormationActionEditor
    {
        private ValidatingTextBox txtStackName;

        public override void BindToForm(ActionBase extension)
        {
            this.EnsureChildControls();
            base.BindToForm(extension);
            var action = extension as WaitForStackCreationAction;
            if (null != action)
            {
                txtStackName.Text = action.StackName;
            }
        }

        public override ActionBase CreateFromForm()
        {
            this.EnsureChildControls();
            return new WaitForStackCreationAction
            {
                EncryptKeys = chkEncryptKeys.Checked,
                AccessKeyCleartext = txtAccessKey.Text,
                SecretKeyCleartext = txtSecretKey.Text,
                Region = ddlRegion.SelectedValue, 
                StackName = txtStackName.Text
            };
        }

        protected override void CreateChildControls()
        {
            base.CreateChildControls();
            txtStackName = new ValidatingTextBox { Required = true, Width = 400 };
            this.Controls.Add(
                new FormFieldGroup("Stack Options", "Stack Options", false,
                    new StandardFormField("Stack Name", txtStackName)
                )
            );
        }
    }
}
