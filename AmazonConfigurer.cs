using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Configurers.Extension;
using Inedo.BuildMaster.Web;

[assembly: ExtensionConfigurer(typeof(Inedo.BuildMasterExtensions.Amazon.AmazonConfigurer))]

namespace Inedo.BuildMasterExtensions.Amazon
{
    /// <summary>
    /// Contains configuration settings for the Amazon extension.
    /// </summary>
    [CustomEditor(typeof(AmazonConfigurerEditor))]
    public sealed class AmazonConfigurer : ExtensionConfigurerBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AmazonConfigurer"/> class.
        /// </summary>
        public AmazonConfigurer()
        {
            this.S3PartSize = 5;
            this.RegionEndpoint = global::Amazon.RegionEndpoint.USWest1.SystemName;
        }

        /// <summary>
        /// Gets or sets the EC2 access key ID (user name).
        /// </summary>
        [Persistent]
        public string AccessKeyId { get; set; }
        /// <summary>
        /// Gets or sets the EC2 secret access key (password).
        /// </summary>
        [Persistent]
        public string SecretAccessKey { get; set; }
        /// <summary>
        /// Gets or sets the part size (in MB) used for multipart transfers.
        /// </summary>
        [Persistent]
        public int S3PartSize { get; set; }
        /// <summary>
        /// Gets or sets the region endpoint.
        /// </summary>
        [Persistent]
        public string RegionEndpoint { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Empty;
        }
    }
}
