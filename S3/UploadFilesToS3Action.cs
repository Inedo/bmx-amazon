using System;
using System.IO;
using Amazon.S3;
using Amazon.S3.Model;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Files;
using Inedo.BuildMaster.Web;
using Inedo.Linq;

namespace Inedo.BuildMasterExtensions.Amazon.S3
{
    /// <summary>
    /// Action for transferring files to an Amazon S3 Bucket.
    /// </summary>
    [ActionProperties(
        "Upload Files to S3",
        "Transfers files to an Amazon S3 bucket.",
        "Amazon")]
    [CustomEditor(typeof(UploadFilesToS3ActionEditor))]
    public sealed class UploadFilesToS3Action : RemoteActionBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UploadFilesToS3Action"/> class.
        /// </summary>
        public UploadFilesToS3Action()
        {
        }

        /// <summary>
        /// Gets or sets the source files masks.
        /// </summary>
        [Persistent]
        public string[] FileMasks { get; set; }
        /// <summary>
        /// Gets or sets the target directory.
        /// </summary>
        [Persistent]
        public string KeyPrefix { get; set; }
        /// <summary>
        /// Gets or sets the bucket name.
        /// </summary>
        [Persistent]
        public string BucketName { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether to use reduced redundancy storage.
        /// </summary>
        [Persistent]
        public bool ReducedRedundancy { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whther the files should be publicly accessible.
        /// </summary>
        [Persistent]
        public bool MakePublic { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether to use server-side encryption.
        /// </summary>
        [Persistent]
        public bool Encrypted { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether to upload files in subfolders.
        /// </summary>
        [Persistent]
        public bool Recursive { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format(
                "Upload files matching ({0}) from {1} to {2}",
                this.FileMasks != null ? string.Join(", ", this.FileMasks) : string.Empty,
                string.IsNullOrEmpty(this.OverriddenSourceDirectory) ? "(default)" : this.OverriddenSourceDirectory,
                this.BucketName + Util.ConcatNE("/", this.KeyPrefix)
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
            this.ExecuteRemoteCommand("upload");
        }
        protected override string ProcessRemoteCommand(string name, string[] args)
        {
            if (name != "upload")
                throw new ArgumentException("Invalid command.");

            var entryResults = Util.Files.GetDirectoryEntry(
                new GetDirectoryEntryCommand
                {
                    Path = this.RemoteConfiguration.SourceDirectory,
                    IncludeRootPath = true,
                    Recurse = this.Recursive
                }
            );

            var matches = Util.Files.Comparison.GetMatches(
                this.RemoteConfiguration.SourceDirectory,
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
            var cfg = (AmazonConfigurer)this.GetExtensionConfigurer();
            if (string.IsNullOrEmpty(cfg.AccessKeyId) || string.IsNullOrEmpty(cfg.SecretAccessKey))
            {
                this.LogError("Amazon Web Services access key and secret access key have not been specified.");
                return string.Empty;
            }

            var prefix = string.Empty;
            if (!string.IsNullOrEmpty(this.KeyPrefix))
                prefix = this.KeyPrefix.Trim('/') + "/";

            using (var s3 = new AmazonS3Client(cfg.AccessKeyId, cfg.SecretAccessKey))
            {
                var uploader = new S3Uploader(
                    s3,
                    this.BucketName,
                    this.ReducedRedundancy ? S3StorageClass.ReducedRedundancy : S3StorageClass.Standard,
                    cfg.S3PartSize * 1024 * 1024,
                    this.MakePublic,
                    this.Encrypted);

                foreach (var fileInfo in matches)
                {
                    var keyName = prefix + fileInfo.Path.Substring(this.RemoteConfiguration.SourceDirectory.Length).Replace(Path.DirectorySeparatorChar, '/').Trim('/');
                    this.LogInformation("Transferring {0} to {1}...", fileInfo.Path, keyName);
                    try
                    {
                        uploader.UploadFile(fileInfo.Path, keyName);
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
