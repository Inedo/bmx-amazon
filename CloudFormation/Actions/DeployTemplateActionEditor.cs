using System;
using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls;
using Inedo.Web.Controls;
using System.Linq.Expressions;
using System.Linq;

namespace Inedo.BuildMasterExtensions.Amazon.CloudFormation
{
    public sealed class DeployTemplateActionEditor : CloudFormationActionEditor
    {
        private ValidatingTextBox txtBucketName;
        private ValidatingTextBox txtTemplateFile;
        private TextBox txtParams;
        private TextBox txtTemplate;
        private ValidatingTextBox txtStackName;
        private TextBox txtTags;
        private TextBox txtCapabilities;
        private CheckBox chkWaitUntilComplete;
        private DropDownList ddlFailureAction;

        private DeployTemplateAction action;

        public static string GetPropertyName<T>(Expression<Func<T>> propertyExpression)
        {
            return (propertyExpression.Body as MemberExpression).Member.Name;
        }

        public override void BindToForm(ActionBase extension)
        {
            this.EnsureChildControls();
            action = extension as DeployTemplateAction;
            base.BindToForm(extension);
            if (null != action)
            {
                txtTemplateFile.Text = action.TemplateFile;
                txtBucketName.Text = action.BucketName;
                txtParams.Text = action.Parameters;
                txtTemplate.Text = action.TemplateText;
                txtStackName.Text = action.StackName;
                txtTags.Text = action.Tags;
                txtCapabilities.Text = action.Capabilities;
                chkWaitUntilComplete.Checked = action.WaitUntilComplete;
                ddlFailureAction.SelectedValue = action.ActionOnFail.ToString();
            }
        }

        public override ActionBase CreateFromForm()
        {
            this.EnsureChildControls();
            return new DeployTemplateAction { EncryptKeys = chkEncryptKeys.Checked, AccessKeyCleartext = txtAccessKey.Text, SecretKeyCleartext = txtSecretKey.Text,
                TemplateFile = txtTemplateFile.Text, 
                BucketName = txtBucketName.Text, Parameters = txtParams.Text, TemplateText = txtTemplate.Text, StackName = txtStackName.Text, Tags = txtTags.Text, 
                Capabilities = txtCapabilities.Text, WaitUntilComplete = chkWaitUntilComplete.Checked, 
                ActionOnFail = (DeployTemplateAction.FailureAction) Enum.Parse(typeof(DeployTemplateAction.FailureAction),ddlFailureAction.SelectedValue),
                Region = ddlRegion.SelectedValue 
            };
        }

        protected override void CreateChildControls()
        {
            base.CreateChildControls();
            txtBucketName = new ValidatingTextBox { Width = 400 };
            txtTemplateFile = new ValidatingTextBox { Width = 400 };
            txtParams = new TextBox { TextMode = TextBoxMode.MultiLine, Rows = 3, Width = 400 };
            txtTemplate = new TextBox { TextMode = TextBoxMode.MultiLine, Rows = 3, Width = 400 };
            txtStackName = new ValidatingTextBox { Required = true, Width = 400 };
            txtTags = new TextBox { TextMode = TextBoxMode.MultiLine, Rows = 3, Width = 400 };
            txtCapabilities = new TextBox { TextMode = TextBoxMode.MultiLine, Rows = 3, Width = 400 };
            chkWaitUntilComplete = new CheckBox { Width = 400 };
            ddlFailureAction = new DropDownList { Width = 400 };
            ddlFailureAction.Items.AddRange( 
                (from object a in Enum.GetValues(typeof(DeployTemplateAction.FailureAction)) select new ListItem {Text = a.ToString(), Value = a.ToString()}).ToArray()
            );
            this.Controls.Add(
                new FormFieldGroup("Template","Template Options, fill out either the S3 URL, the name of a config file to use as the template or the full text of a template. The template that is deployed is selected in that order.",false,
                    new StandardFormField("S3 URL", txtBucketName),
                    new StandardFormField("Template Config File", txtTemplateFile),
                    new StandardFormField("Template Data", txtTemplate)
                ),
                new FormFieldGroup("Stack Options","Stack Options",false,
                    new StandardFormField("Stack Name",txtStackName),
                    new StandardFormField("Parameters", txtParams),
                    new StandardFormField("Tags",txtTags),
                    new StandardFormField("Capabilities",txtCapabilities)                    
                ),
                new FormFieldGroup("Deploy","Deploy Options",false,
                    new StandardFormField("Action to take on failure",ddlFailureAction),
                    new StandardFormField("Wait Until Complete",chkWaitUntilComplete)
                    )
            );
        }
    }
}
