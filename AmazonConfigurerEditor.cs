using Inedo.BuildMaster.Extensibility.Configurers.Extension;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.Amazon
{
    /// <summary>
    /// Custom editor for the Amazon EC2 configurer class.
    /// </summary>
    internal sealed class AmazonConfigurerEditor : ExtensionConfigurerEditorBase
    {
        private ValidatingTextBox txtKeyId;
        private ValidatingTextBox txtSecretKey;
        private NumericTextBox txtPartSize;

        /// <summary>
        /// Initializes a new instance of the <see cref="AmazonConfigurerEditor"/> class.
        /// </summary>
        public AmazonConfigurerEditor()
        {
        }

        protected override void CreateChildControls()
        {
            base.CreateChildControls();

            this.txtKeyId = new ValidatingTextBox { Required = true, Width = 300 };
            this.txtSecretKey = new PasswordTextBox { Required = true, Width = 250 };
            this.txtPartSize = new NumericTextBox { MinValue = 5, MaxValue = 20 };

            CUtil.Add(this,
                new FormFieldGroup(
                    "Credentials",
                    "Specifies the credentials used to authenticate with Amazon Web Services. " +
                    "For more information about EC2 security credentials, visit " +
                    "<a href=\"http://docs.amazonwebservices.com/AWSSecurityCredentials/1.0/AboutAWSCredentials.html\" target=\"_blank\">" +
                    "Amazon's documentation</a>.",
                    false,
                    new StandardFormField(
                        "Access Key ID:",
                        this.txtKeyId
                    ),
                    new StandardFormField(
                        "Secret Access Key:",
                        this.txtSecretKey
                    )
                ),
                new FormFieldGroup(
                    "S3 Part Size",
                    "Files larger than the value specified here will be split into multiple parts and " +
                    "uploaded using a multipart transfer.",
                    true,
                    new StandardFormField(
                        "Part Size (MB):",
                        this.txtPartSize
                    )
                )
            );
        }

        public override void BindToForm(ExtensionConfigurerBase extension)
        {
            this.EnsureChildControls();

            var cfg = (AmazonConfigurer)extension;
            this.txtKeyId.Text = cfg.AccessKeyId;
            this.txtSecretKey.Text = cfg.SecretAccessKey;
        }
        public override ExtensionConfigurerBase CreateFromForm()
        {
            this.EnsureChildControls();

            return new AmazonConfigurer
            {
                AccessKeyId = this.txtKeyId.Text,
                SecretAccessKey = this.txtSecretKey.Text,
                S3PartSize = (int)this.txtPartSize.Value
            };
        }
        /// <summary>
        /// Populates the fields within the control with the appropriate default values.
        /// </summary>
        /// <remarks>
        /// This is only called when creating a new extension.
        /// </remarks>
        public override void InitializeDefaultValues()
        {
            BindToForm(new AmazonConfigurer());
        }
    }
}
