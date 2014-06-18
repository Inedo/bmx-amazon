using System;
using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;
using Inedo.Web.Controls.SimpleHtml;

namespace Inedo.BuildMasterExtensions.Amazon.S3
{
    internal sealed class UploadFilesToS3ActionEditor : ActionEditorBase
    {
        private TextBox txtFileMasks;
        private TextBox txtPrefix;
        private ValidatingTextBox txtBucket;
        private CheckBox chkReducedRedundancy;
        private CheckBox chkPublic;
        private CheckBox chkEncrypted;
        private CheckBox chkRecursive;

        public override bool DisplaySourceDirectory
        {
            get { return true; }
        }
        public override string ServerLabel
        {
            get { return "From:"; }
        }
        public override string SourceDirectoryLabel
        {
            get { return "In:"; }
        }

        public override void BindToForm(ActionBase extension)
        {
            this.EnsureChildControls();

            var upload = (UploadFilesToS3Action)extension;
            this.txtFileMasks.Text = string.Join(Environment.NewLine, upload.FileMasks ?? new string[0]);
            this.txtPrefix.Text = upload.KeyPrefix;
            this.txtBucket.Text = upload.BucketName;
            this.chkReducedRedundancy.Checked = upload.ReducedRedundancy;
            this.chkPublic.Checked = upload.MakePublic;
            this.chkEncrypted.Checked = upload.Encrypted;
            this.chkRecursive.Checked = upload.Recursive;
        }
        public override ActionBase CreateFromForm()
        {
            this.EnsureChildControls();

            return new UploadFilesToS3Action
            {
                FileMasks = this.txtFileMasks.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries),
                KeyPrefix = this.txtPrefix.Text,
                BucketName = this.txtBucket.Text,
                ReducedRedundancy = this.chkReducedRedundancy.Checked,
                MakePublic = this.chkPublic.Checked,
                Encrypted = this.chkEncrypted.Checked,
                Recursive = this.chkRecursive.Checked
            };
        }

        protected override void CreateChildControls()
        {
            this.txtFileMasks = new ValidatingTextBox
            {
                TextMode = TextBoxMode.MultiLine,
                Rows = 5
            };

            this.txtPrefix = new ValidatingTextBox();

            this.txtBucket = new ValidatingTextBox
            {
                Required = true
            };

            this.chkReducedRedundancy = new CheckBox
            {
                Text = "Reduced redundancy"
            };

            this.chkPublic = new CheckBox
            {
                Text = "Make public"
            };

            this.chkEncrypted = new CheckBox
            {
                Text = "Server-side encryption"
            };

            this.chkRecursive = new CheckBox
            {
                Text = "Also match files in subdirectories"
            };

            this.Controls.Add(
                new SlimFormField("Matching files:", new Div(this.txtFileMasks), new Div(this.chkRecursive)),
                new SlimFormField("To bucket:", this.txtBucket),
                new SlimFormField("To folder:", this.txtPrefix),
                new SlimFormField(
                    "Options",
                    new Div(this.chkPublic),
                    new Div(this.chkReducedRedundancy),
                    new Div(this.chkEncrypted)
                )
            );
        }
    }
}
