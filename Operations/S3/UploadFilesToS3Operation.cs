using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Documentation;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Agents;
using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.Diagnostics;
using Inedo.IO;

namespace Inedo.BuildMasterExtensions.Amazon.Operations.S3
{
    [ScriptAlias("Upload-Files")]
    [DisplayName("Upload Files to S3")]
    [Description("Transfers files to an Amazon S3 bucket.")]
    [ScriptNamespace("AWS")]
    [Tag("amazon"), Tag("cloud")]
    public sealed class UploadFilesToS3Operation : ExecuteOperation
    {
        private long totalUploadBytes;
        private long uploadedBytes;

        [ScriptAlias("From")]
        [DisplayName("Source directory")]
        [Description(CommonDescriptions.SourceDirectory)]
        public string SourceDirectory { get; set; }
        [ScriptAlias("Include")]
        [DisplayName("Include")]
        [Description(CommonDescriptions.IncludeMask)]
        public IEnumerable<string> Includes { get; set; }
        [ScriptAlias("Exclude")]
        [DisplayName("Exclude")]
        [Description(CommonDescriptions.ExcludeMask)]
        public IEnumerable<string> Excludes { get; set; }

        [Required]
        [ScriptAlias("Bucket")]
        [DisplayName("Bucket")]
        [Description("The name of the S3 bucket that will receive the uploaded files.")]
        public string BucketName { get; set; }
        [ScriptAlias("To")]
        [DisplayName("Target folder")]
        [Description("The directory in the specified S3 bucket that will received the uploaded files.")]
        public string KeyPrefix { get; set; }
        [ScriptAlias("ReducedRedundancy")]
        [DisplayName("Use reduced redundancy")]
        [Description("When true, files uploaded will use reduced redundancy storage.")]
        [Category("Storage")]
        public bool ReducedRedundancy { get; set; }
        [ScriptAlias("Public")]
        [Description("When true, files uploaded will be publicly visible.")]
        [DisplayName("Make public")]
        [Category("Storage")]
        public bool MakePublic { get; set; }
        [ScriptAlias("Encrypted")]
        [Description("When true, files uploaded will be protected with server side encryption.")]
        [Category("Storage")]
        public bool Encrypted { get; set; }

        [Required]
        [ScriptAlias("AccessKey")]
        [DisplayName("Access key")]
        public string AccessKey { get; set; }
        [Required]
        [ScriptAlias("SecretAccessKey")]
        [DisplayName("Secret access key")]
        public string SecretAccessKey { get; set; }

        [Category("Network")]
        [ScriptAlias("RegionEndpoint")]
        [DisplayName("Region endpoint")]
        public string RegionEndpoint { get; set; }
        [Category("Network")]
        [ScriptAlias("PartSize")]
        [DisplayName("Part size")]
        [DefaultValue(5L * 1024 * 1024)]
        [Description("The size (in bytes) of individual parts for an S3 multipart upload.")]
        public long PartSize { get; set; } = 5L * 1024 * 1024;

        private S3CannedACL CannedACL => this.MakePublic ? S3CannedACL.PublicRead : S3CannedACL.NoACL;
        private ServerSideEncryptionMethod EncryptionMethod => this.Encrypted ? ServerSideEncryptionMethod.AES256 : ServerSideEncryptionMethod.None;
        private S3StorageClass StorageClass => this.ReducedRedundancy ? S3StorageClass.ReducedRedundancy : S3StorageClass.Standard;

        public override async Task ExecuteAsync(IOperationExecutionContext context)
        {
            var fileOps = context.Agent.GetService<IFileOperationsExecuter>();
            var sourceDirectory = context.ResolvePath(this.SourceDirectory);
            if (!fileOps.DirectoryExists(sourceDirectory))
            {
                this.LogWarning($"Source directory {sourceDirectory} does not exist; nothing to upload.");
                return;
            }

            var files = fileOps.GetFileSystemInfos(sourceDirectory, new MaskingContext(this.Includes, this.Excludes))
                .OfType<SlimFileInfo>()
                .ToList();

            if (files.Count == 0)
            {
                this.LogWarning($"No files match the specified masks in {sourceDirectory}; nothing to upload.");
                return;
            }

            var prefix = string.Empty;
            if (!string.IsNullOrEmpty(this.KeyPrefix))
                prefix = this.KeyPrefix.Trim('/') + "/";

            Interlocked.Exchange(ref this.totalUploadBytes, files.Sum(f => f.Size));

            using (var s3 = this.CreateClient())
            {
                foreach (var file in files)
                {
                    var keyName = prefix + file.FullName.Substring(sourceDirectory.Length).Replace(Path.DirectorySeparatorChar, '/').Trim('/');
                    this.LogInformation($"Transferring {file.FullName} to {keyName} ({FormatSize(file.Size)})...");
                    using (var fileStream = fileOps.OpenFile(file.FullName, FileMode.Open, FileAccess.Read))
                    {
                        if (file.Size < this.PartSize * 2)
                            await this.UploadSmallFileAsync(s3, fileStream, keyName, context);
                        else
                            await this.MultipartUploadAsync(s3, fileStream, keyName, context);
                    }
                }
            }
        }

        public override OperationProgress GetProgress()
        {
            long total = Interlocked.Read(ref this.totalUploadBytes);
            long uploaded = Interlocked.Read(ref this.uploadedBytes);
            if (total == 0)
                return null;

            long remaining = Math.Max(total - uploaded, 0);
            if (remaining > 0)
                return new OperationProgress((int)(100.0 * uploaded / total), FormatSize(remaining) + " remaining");
            else
                return new OperationProgress(100);
        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            return new ExtendedRichDescription(
                new RichDescription(
                    "Upload ",
                    new MaskHilite(config[nameof(this.Includes)], config[nameof(this.Excludes)]),
                    " to S3"
                ),
                new RichDescription(
                    "from ",
                    new DirectoryHilite(config[nameof(this.SourceDirectory)]),
                    " to ",
                    new Hilite(config[nameof(this.BucketName)] + Util.ConcatNE("/", config[nameof(this.KeyPrefix)]))
                )
            );
        }

        private Task UploadSmallFileAsync(AmazonS3Client s3, Stream stream, string key, IOperationExecutionContext context)
        {
            return s3.PutObjectAsync(
                new PutObjectRequest
                {
                    BucketName = this.BucketName,
                    Key = key,
                    AutoCloseStream = false,
                    AutoResetStreamPosition = false,
                    InputStream = stream,
                    CannedACL = this.CannedACL,
                    StorageClass = this.StorageClass,
                    ServerSideEncryptionMethod = this.EncryptionMethod,
                    StreamTransferProgress = (s, e) => Interlocked.Add(ref this.uploadedBytes, e.IncrementTransferred)
                },
                context.CancellationToken
            );
        }
        private async Task MultipartUploadAsync(AmazonS3Client s3, Stream stream, string key, IOperationExecutionContext context)
        {
            var uploadResponse = await s3.InitiateMultipartUploadAsync(
                new InitiateMultipartUploadRequest
                {
                    BucketName = this.BucketName,
                    Key = key,
                    CannedACL = this.CannedACL,
                    StorageClass = this.StorageClass,
                    ServerSideEncryptionMethod = this.EncryptionMethod
                },
                context.CancellationToken
            );

            try
            {
                var parts = this.GetParts(stream.Length);

                var completedParts = new List<PartETag>(parts.Count);
                for (int i = 0; i < parts.Count; i++)
                {
                    var partResponse = await s3.UploadPartAsync(
                        new UploadPartRequest
                        {
                            BucketName = this.BucketName,
                            Key = key,
                            InputStream = stream,
                            UploadId = uploadResponse.UploadId,
                            PartSize = parts[i].Length,
                            FilePosition = parts[i].StartOffset,
                            PartNumber = i + 1,
                            StreamTransferProgress = (s, e) => Interlocked.Add(ref this.uploadedBytes, e.IncrementTransferred)
                        },
                        context.CancellationToken
                    );

                    completedParts.Add(new PartETag(i + 1, partResponse.ETag));
                }

                await s3.CompleteMultipartUploadAsync(
                    new CompleteMultipartUploadRequest
                    {
                        BucketName = this.BucketName,
                        Key = key,
                        UploadId = uploadResponse.UploadId,
                        PartETags = completedParts
                    },
                    context.CancellationToken
                );
            }
            catch
            {
                await s3.AbortMultipartUploadAsync(
                    new AbortMultipartUploadRequest
                    {
                        BucketName = this.BucketName,
                        Key = key,
                        UploadId = uploadResponse.UploadId
                    }
                );

                throw;
            }
        }
        private List<PartInfo> GetParts(long totalSize)
        {
            if (totalSize < this.PartSize * 2)
                return null;

            int wholeParts = (int)(totalSize / this.PartSize);
            var parts = new List<PartInfo>(wholeParts);

            for (int i = 0; i < wholeParts - 1; i++)
                parts.Add(new PartInfo { StartOffset = i * this.PartSize, Length = this.PartSize });

            long remainder = totalSize % this.PartSize;
            parts.Add(new PartInfo { StartOffset = (wholeParts - 1) * this.PartSize, Length = this.PartSize + remainder });

            return parts;
        }
        private static string FormatSize(long size)
        {
            if (size < 1024L)
                return size.ToString("G") + " b";

            double s = size;

            if (size < 10L * 1024 * 1024)
                return (s / 1024.0).ToString("##,#") + " KB";

            if (size < 10L * 1024 * 1024 * 1024)
                return (s / (1024.0 * 1024.0)).ToString("##,#.#") + " MB";

            return (s / (1024.0 * 1024.0 * 1024.0)).ToString("##,#.##") + " GB";
        }
        private AmazonS3Client CreateClient()
        {
            return string.IsNullOrWhiteSpace(this.RegionEndpoint)
                ? new AmazonS3Client(this.AccessKey, this.SecretAccessKey)
                : new AmazonS3Client(this.AccessKey, this.SecretAccessKey, global::Amazon.RegionEndpoint.GetBySystemName(this.RegionEndpoint));
        }

        private struct PartInfo
        {
            public long StartOffset { get; set; }
            public long Length { get; set; }
        }
    }
}
