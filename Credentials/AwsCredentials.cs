using System.ComponentModel;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Credentials;
using Inedo.BuildMaster.Web;
using Inedo.Documentation;
using Inedo.Serialization;

namespace Inedo.BuildMasterExtensions.Amazon.Credentials
{
    [ScriptAlias("AWS")]
    [DisplayName("AWS")]
    [Description("Credentials that represent an access key ID and secret key.")]
    public sealed class AwsCredentials : ResourceCredentials
    {
        [Persistent]
        [DisplayName("Access key")]
        public string AccessKeyId { get; set; }
        [Persistent(Encrypted = true)]
        [FieldEditMode(FieldEditMode.Password)]
        [DisplayName("Secret access key")]
        public string SecretAccessKey { get; set; }

        public override RichDescription GetDescription()
        {
            return new RichDescription("Access key: ", new Hilite(this.AccessKeyId));
        }
    }
}
