using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Amazon.S3;
using Amazon.S3.Model;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Files;
using Inedo.BuildMaster.Web;
using Inedo.BuildMasterExtensions.Amazon.Legacy.ActionImporters;
using Inedo.Documentation;
using Inedo.Serialization;

namespace Inedo.BuildMasterExtensions.Amazon.S3
{
    [DisplayName("Upload Files to S3")]
    [Description("Transfers files to an Amazon S3 bucket.")]
    [CustomEditor(typeof(UploadFilesToS3ActionEditor))]
    [Tag("amazon"), Tag("cloud")]
    [ConvertibleToOperation(typeof(UploadToS3Importer))]
    public sealed class UploadFilesToS3Action : AgentBasedActionBase
    {
        [Persistent]
        public string[] FileMasks { get; set; }
        [Persistent]
        public string KeyPrefix { get; set; }
        [Persistent]
        public string BucketName { get; set; }
        [Persistent]
        public bool ReducedRedundancy { get; set; }
        [Persistent]
        public bool MakePublic { get; set; }
        [Persistent]
        public bool Encrypted { get; set; }
        [Persistent]
        public bool Recursive { get; set; }

        public override ExtendedRichDescription GetActionDescription()
        {
            return new ExtendedRichDescription(
                new RichDescription(
                    "Upload ",
                    new ListHilite(this.FileMasks),
                    " to S3"
                ),
                new RichDescription(
                    "from ",
                    new DirectoryHilite(this.OverriddenSourceDirectory),
                    " to ",
                    new Hilite(this.BucketName + Util.ConcatNE("/", this.KeyPrefix))
                )
            );
        }

        protected override void Execute()
        {
            if (this.FileMasks == null || this.FileMasks.Length == 0)
            {
                this.LogWarning("No file masks are specified; nothing to do.");
                return;
            }

            this.LogInformation("Uploading to {0}...", this.BucketName + Util.ConcatNE("/", this.KeyPrefix));

            var cfg = (AmazonConfigurer)this.GetExtensionConfigurer();
            if (string.IsNullOrEmpty(cfg.AccessKeyId) || string.IsNullOrEmpty(cfg.SecretAccessKey))
            {
                this.LogError("Amazon Web Services access key and secret access key have not been specified.");
                return;
            }
            
            this.ProcessRemoteCommand("upload", cfg.AccessKeyId, cfg.SecretAccessKey, cfg.S3PartSize.ToString(), cfg.RegionEndpoint);
        }
        private string ProcessRemoteCommand(string name, params string[] args)
        {
            if (name != "upload")
                throw new ArgumentException("Invalid command.");

            var accessKeyId = args[0];
            var secretAccessKey = args[1];
            int partSize = int.Parse(args[2]);
            var regionEndpoint = args[3];

            var entryResults = Util.Files.GetDirectoryEntry(
                new GetDirectoryEntryCommand
                {
                    Path = this.Context.SourceDirectory,
                    IncludeRootPath = true,
                    Recurse = this.Recursive
                }
            );

            var matches = Util.Files.Comparison.GetMatches(
                this.Context.SourceDirectory,
                entryResults.Entry,
                this.FileMasks
            ).Where(s => s is FileEntryInfo).ToList();

            if (matches.Count == 0)
            {
                this.LogWarning("No files matched with the specified mask.");
                return string.Empty;
            }

            this.LogDebug("Mask matched {0} file(s).", matches.Count);

            this.LogInformation("Contacting Amazon S3 Service...");

            var prefix = string.Empty;
            if (!string.IsNullOrEmpty(this.KeyPrefix))
                prefix = this.KeyPrefix.Trim('/') + "/";

            using (var s3 = new AmazonS3Client(accessKeyId, secretAccessKey, global::Amazon.RegionEndpoint.GetBySystemName(regionEndpoint)))
            {
                var uploader = new S3Uploader(
                    s3,
                    this.BucketName,
                    this.ReducedRedundancy ? S3StorageClass.ReducedRedundancy : S3StorageClass.Standard,
                    partSize * 1024 * 1024,
                    this.MakePublic,
                    this.Encrypted
                );

                foreach (var fileInfo in matches)
                {
                    var keyName = prefix + fileInfo.Path.Substring(this.Context.SourceDirectory.Length).Replace(Path.DirectorySeparatorChar, '/').Trim('/');
                    this.LogInformation("Transferring {0} to {1}...", fileInfo.Path, keyName);
                    try
                    {
                        uploader.UploadFile(fileInfo.Path, keyName, p => this.LogDebug("{0}% transferred.", p.Percent));
                        this.LogDebug("Upload complete!");
                    }
                    catch (Exception ex)
                    {
                        if (this.ResumeNextOnError)
                            this.LogError("Upload failed: {0}", ex.Message);
                        else
                            throw;
                    }
                }
            }

            return string.Empty;
        }
    }
}
