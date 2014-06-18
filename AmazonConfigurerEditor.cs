using System.Linq;
using System.Web.UI.WebControls;
using Amazon;
using Inedo.BuildMaster.Extensibility.Configurers.Extension;
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
        private ValidatingTextBox txtPartSize;
        private DropDownList ddlRegionEndpoint;

        protected override void CreateChildControls()
        {
            this.txtKeyId = new ValidatingTextBox { Required = true, Width = 300 };
            this.txtSecretKey = new PasswordTextBox { Required = true, Width = 250 };
            this.txtPartSize = new ValidatingTextBox { Type = ValidationDataType.Integer, Text = "5" };
            this.ddlRegionEndpoint = new DropDownList { ID = "ddlRegionEndpoint" };
            this.ddlRegionEndpoint.Items.AddRange(RegionEndpoint.EnumerableAllRegions.Select(r => new ListItem(r.DisplayName, r.SystemName)));

            this.Controls.Add(
                new SlimFormField("Access key ID:", this.txtKeyId),
                new SlimFormField("Secret access key:", this.txtSecretKey),
                new SlimFormField("S3 part size:", this.txtPartSize),
                new SlimFormField("Endpoint:", this.ddlRegionEndpoint)
            );
        }

        public override void BindToForm(ExtensionConfigurerBase extension)
        {
            this.EnsureChildControls();

            var cfg = (AmazonConfigurer)extension;
            this.txtKeyId.Text = cfg.AccessKeyId;
            this.txtSecretKey.Text = cfg.SecretAccessKey;
            this.txtPartSize.Text = cfg.S3PartSize.ToString();
            this.ddlRegionEndpoint.SelectedValue = cfg.RegionEndpoint;
        }
        public override ExtensionConfigurerBase CreateFromForm()
        {
            this.EnsureChildControls();

            return new AmazonConfigurer
            {
                AccessKeyId = this.txtKeyId.Text,
                SecretAccessKey = this.txtSecretKey.Text,
                S3PartSize = int.Parse(this.txtPartSize.Text),
                RegionEndpoint = this.ddlRegionEndpoint.SelectedValue
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
            this.BindToForm(new AmazonConfigurer());
        }
    }
}
