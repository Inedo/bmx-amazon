using System;
using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.Amazon.S3
{
    /// <summary>
    /// Custom editor for the <see cref="UploadFilesToS3Action"/> class.
    /// </summary>
    internal sealed class UploadFilesToS3ActionEditor : ActionEditorBase
    {
        private TextBox txtFileMasks;
        private TextBox txtPrefix;
        private ValidatingTextBox txtBucket;
        private CheckBox chkReducedRedundancy;
        private CheckBox chkPublic;
        private CheckBox chkEncrypted;
        private CheckBox chkRecursive;

        /// <summary>
        /// Initializes a new instance of the <see cref="UploadFilesToS3ActionEditor"/> class.
        /// </summary>
        public UploadFilesToS3ActionEditor()
        {
        }

        public override bool DisplaySourceDirectory
        {
            get { return true; }
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
            base.CreateChildControls();

            this.txtFileMasks = new TextBox
            {
                Width = 300,
                TextMode = TextBoxMode.MultiLine,
                Rows = 5
            };

            this.txtPrefix = new TextBox { Width = 300 };

            this.txtBucket = new ValidatingTextBox
            {
                Width = 300,
                Required = true
            };

            this.chkReducedRedundancy = new CheckBox
            {
                Text = "Reduced Redundancy"
            };

            this.chkPublic = new CheckBox
            {
                Text = "Make Public"
            };

            this.chkEncrypted = new CheckBox
            {
                Text = "Server-Side Encryption"
            };

            this.chkRecursive = new CheckBox
            {
                Text = "Recursively upload files from subdirectories"
            };

            CUtil.Add(this,
                new FormFieldGroup(
                    "File Masks",
                    "Files in the source directory that match a mask entered here will (one per line) be uploaded.",
                    false,
                    new StandardFormField(
                        "File Masks:",
                        this.txtFileMasks
                    ),
                    new StandardFormField(
                        string.Empty,
                        this.chkRecursive
                    )
                ),
                new FormFieldGroup(
                    "Bucket",
                    "Specify the name of the S3 bucket to upload files to, and optionally provide a target folder inside the bucket.",
                    false,
                    new StandardFormField(
                        "Bucket Name:",
                        this.txtBucket
                    ),
                    new StandardFormField(
                        "Folder Name:",
                        this.txtPrefix
                    )
                ),
                new FormFieldGroup(
                    "Options",
                    "Use these options to configure storage options and accessibility of the uploaded files.",
                    true,
                    new StandardFormField(
                        string.Empty,
                        this.chkPublic
                    ),
                    new StandardFormField(
                        string.Empty,
                        this.chkReducedRedundancy
                    ),
                    new StandardFormField(
                        string.Empty,
                        this.chkEncrypted
                    )
                )
            );
        }
    }
}
