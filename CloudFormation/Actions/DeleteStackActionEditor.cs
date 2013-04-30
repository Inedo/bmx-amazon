using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.UI.WebControls;

using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls;
using Inedo.Web.Controls;

namespace CloudFormation.Actions
{
    public class DeleteStackActionEditor : CloudFormationActionEditor 
    {
        private ValidatingTextBox txtStackName;
        private CheckBox chkWaitUntilComplete;

        public override void BindToForm(ActionBase extension)
        {
            this.EnsureChildControls();
            base.BindToForm(extension);
            var action = extension as DeleteStackAction;
            if (null != action)
            {
                txtStackName.Text = action.StackName;
                chkWaitUntilComplete.Checked = action.WaitUntilComplete;
            }
        }

        public override ActionBase CreateFromForm()
        {
            this.EnsureChildControls();
            return new DeleteStackAction
            {
                EncryptKeys = chkEncryptKeys.Checked,
                AccessKeyCleartext = txtAccessKey.Text,
                SecretKeyCleartext = txtSecretKey.Text,
                Region = ddlRegion.SelectedValue, 
                StackName = txtStackName.Text,
                WaitUntilComplete = chkWaitUntilComplete.Checked
            };
        }

        protected override void CreateChildControls()
        {
            base.CreateChildControls();
            txtStackName = new ValidatingTextBox { Required = true, Width = 400 };
            chkWaitUntilComplete = new CheckBox { Width = 400 };
            this.Controls.Add(
                new FormFieldGroup("Stack Options", "Stack Options", false,
                    new StandardFormField("Stack Name", txtStackName)
                ),
                new FormFieldGroup("Deploy", "Deploy Options", false,
                    new StandardFormField("Wait Until Complete", chkWaitUntilComplete)
                )
            );
        }
    }
}
