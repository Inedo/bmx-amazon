using Inedo.BuildMaster.Extensibility.Configurers.Extension;
using Inedo.Serialization;
using Inedo.Web;

[assembly: ExtensionConfigurer(typeof(Inedo.BuildMasterExtensions.Amazon.AmazonConfigurer))]

namespace Inedo.BuildMasterExtensions.Amazon
{
    [SlimSerializable]
    [CustomEditor(typeof(AmazonConfigurerEditor))]
    [PersistFrom("Inedo.BuildMasterExtensions.Amazon.AmazonConfigurer,Amazon")]
    public sealed class AmazonConfigurer : ExtensionConfigurerBase
    {
        [Persistent]
        public string AccessKeyId { get; set; }
        [Persistent(Encrypted = true)]
        public string SecretAccessKey { get; set; }
        [Persistent]
        public int S3PartSize { get; set; } = 5;
        [Persistent]
        public string RegionEndpoint { get; set; } = global::Amazon.RegionEndpoint.USWest1.SystemName;
    }
}
