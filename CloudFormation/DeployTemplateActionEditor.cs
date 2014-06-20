using System;
using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.ClientResources;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.Amazon.CloudFormation
{
    internal sealed class DeployTemplateActionEditor : ActionEditorBase
    {
        private DropDownList ddlTemplateMode;
        private ValidatingTextBox txtBucketName;
        private ValidatingTextBox txtTemplateFile;
        private TextBox txtParams;
        private TextBox txtTemplate;
        private ValidatingTextBox txtStackName;
        private TextBox txtTags;
        private TextBox txtCapabilities;
        private CheckBox chkWaitUntilComplete;
        private DropDownList ddlFailureAction;

        public override void BindToForm(ActionBase extension)
        {
            var action = (DeployTemplateAction)extension as DeployTemplateAction;
            this.txtTemplateFile.Text = action.TemplateFile;
            this.txtBucketName.Text = action.BucketName;
            this.txtParams.Text = action.Parameters;
            this.txtTemplate.Text = action.TemplateText;
            this.txtStackName.Text = action.StackName;
            this.txtTags.Text = action.Tags;
            this.txtCapabilities.Text = action.Capabilities;
            this.chkWaitUntilComplete.Checked = action.WaitUntilComplete;
            this.ddlFailureAction.SelectedValue = action.ActionOnFail.ToString();

            if (!string.IsNullOrEmpty(action.BucketName))
                this.ddlTemplateMode.SelectedValue = "s3";
            else if (!string.IsNullOrEmpty(action.TemplateFile))
                this.ddlTemplateMode.SelectedValue = "config";
            else if (!string.IsNullOrEmpty(action.TemplateText))
                this.ddlTemplateMode.SelectedValue = "direct";
        }
        public override ActionBase CreateFromForm()
        {
            return new DeployTemplateAction
            {
                TemplateFile = this.ddlTemplateMode.SelectedValue == "config" ? this.txtTemplateFile.Text : null,
                BucketName = this.ddlTemplateMode.SelectedValue == "s3" ? this.txtBucketName.Text : null,
                TemplateText = this.ddlTemplateMode.SelectedValue == "direct" ? this.txtTemplate.Text : null,
                Parameters = this.txtParams.Text,
                StackName = this.txtStackName.Text,
                Tags = this.txtTags.Text,
                Capabilities = this.txtCapabilities.Text,
                WaitUntilComplete = this.chkWaitUntilComplete.Checked,
                ActionOnFail = (DeployTemplateAction.FailureAction)Enum.Parse(typeof(DeployTemplateAction.FailureAction), this.ddlFailureAction.SelectedValue),
            };
        }

        protected override void CreateChildControls()
        {
            this.ddlTemplateMode = new DropDownList
            {
                ID = "ddlTemplateMode",
                Items =
                {
                    new ListItem("S3 Bucket", "s3"),
                    new ListItem("BuildMaster Configuration File", "config"),
                    new ListItem("Direct Entry", "direct")
                },
                SelectedValue = "s3"
            };

            this.txtBucketName = new ValidatingTextBox();
            this.txtTemplateFile = new ValidatingTextBox();
            this.txtParams = new TextBox { TextMode = TextBoxMode.MultiLine, Rows = 3 };
            this.txtTemplate = new TextBox { TextMode = TextBoxMode.MultiLine, Rows = 3 };
            this.txtStackName = new ValidatingTextBox { Required = true };
            this.txtTags = new TextBox { TextMode = TextBoxMode.MultiLine, Rows = 3 };
            this.txtCapabilities = new TextBox { TextMode = TextBoxMode.MultiLine, Rows = 3 };
            this.chkWaitUntilComplete = new CheckBox { Text = "Wait until complete" };
            this.ddlFailureAction = new DropDownList
            {
                ID = "ddlFailureAction",
                Items =
                {
                    new ListItem("Do nothing", "DO_NOTHING"),
                    new ListItem("Roll back", "ROLLBACK"),
                    new ListItem("Delete", "DELETE")
                },
                SelectedValue = "DO_NOTHING"
            };

            var ctlS3Container = new SlimFormField("S3 URL:", this.txtBucketName) { ID = "ctlS3Container" };
            var ctlConfigContainer = new SlimFormField("Configuration file:", this.txtTemplateFile) { ID = "ctlConfigContainer" };
            var ctlDirectContainer = new SlimFormField("Template:", this.txtTemplate) { ID = "ctlDirectContainer" };

            this.Controls.Add(
                new SlimFormField("Read template from:", this.ddlTemplateMode),
                ctlS3Container,
                ctlConfigContainer,
                ctlDirectContainer,
                new SlimFormField("Stack name:", this.txtStackName),
                new SlimFormField("Parameters:", this.txtParams),
                new SlimFormField("Tags:", this.txtTags),
                new SlimFormField("Capabilities:", this.txtCapabilities),
                new SlimFormField("Failure action:", this.ddlFailureAction),
                new SlimFormField("Options:", this.chkWaitUntilComplete),
                new RenderJQueryDocReadyDelegator(
                    w =>
                    {
                        w.Write("InitDeployTemplateActionEditor(");
                        InedoLib.Util.JavaScript.WriteJson(
                            w,
                            new
                            {
                                modeSelector = "#" + this.ddlTemplateMode.ClientID,
                                s3Selector = "#" + ctlS3Container.ClientID,
                                configSelector = "#" + ctlConfigContainer.ClientID,
                                directSelector = "#" + ctlDirectContainer.ClientID
                            }
                        );
                        w.Write(");");
                    }
                )
            );
        }
        protected override void OnPreRender(EventArgs e)
        {
            this.IncludeClientResourceInPage(
                new JavascriptResource
                {
                    ResourcePath = "~/extension-resources/Amazon/CloudFormation/DeployTemplateActionEditor.js?" + typeof(DeployTemplateActionEditor).Assembly.GetName().Version,
                    CompatibleVersions = { InedoLibCR.Versions.jq152, InedoLibCR.Versions.jq161, InedoLibCR.Versions.jq171 }
                }
            );

            base.OnPreRender(e);
        }
    }
}
