using System;
using System.Collections.Generic;
using System.IO;
using Amazon.S3;
using Amazon.S3.Model;

namespace Inedo.BuildMasterExtensions.Amazon.S3
{
    internal sealed class S3Uploader
    {
        private AmazonS3Client s3;

        public S3Uploader(AmazonS3Client s3client, string bucketName, S3StorageClass storageClass, long partSize, bool makePublic, bool encrypted)
        {
            this.s3 = s3client;
            this.BucketName = bucketName;
            this.StorageClass = storageClass;
            this.PartSize = partSize;
            this.MakePublic = makePublic;
            this.Encrypted = encrypted;
        }

        public string BucketName { get; private set; }
        public S3StorageClass StorageClass { get; private set; }
        public long PartSize { get; private set; }
        public bool MakePublic { get; private set; }
        public bool Encrypted { get; private set; }

        private S3CannedACL CannedACL
        {
            get { return this.MakePublic ? S3CannedACL.PublicRead : S3CannedACL.NoACL; }
        }
        private ServerSideEncryptionMethod EncryptionMethod
        {
            get { return this.Encrypted ? ServerSideEncryptionMethod.AES256 : ServerSideEncryptionMethod.None; }
        }

        public void UploadFile(string fileName, string keyName)
        {
            long fileSize = new FileInfo(fileName).Length;
            var parts = this.GetParts(fileSize);
            if (parts == null)
            {
                this.s3.PutObject(
                    new PutObjectRequest
                    {
                        BucketName = this.BucketName,
                        Key = keyName,
                        StorageClass = this.StorageClass,
                        GenerateMD5Digest = true,
                        FilePath = fileName,
                        CannedACL = this.CannedACL,
                        ServerSideEncryptionMethod = this.EncryptionMethod
                    }
                );
            }
            else
            {
                var uploadResponse = this.s3.InitiateMultipartUpload(
                    new InitiateMultipartUploadRequest
                    {
                        BucketName = this.BucketName,
                        Key = keyName,
                        StorageClass = this.StorageClass,
                        CannedACL = this.CannedACL,
                        ServerSideEncryptionMethod = this.EncryptionMethod
                    }
                );

                try
                {
                    var completedParts = new List<PartETag>(parts.Count);
                    for (int i = 0; i < parts.Count; i++)
                    {
                        var partResponse = this.s3.UploadPart(
                            new UploadPartRequest
                            {
                                BucketName = this.BucketName,
                                Key = keyName,
                                FilePath = fileName,
                                UploadId = uploadResponse.UploadId,
                                GenerateMD5Digest = true,
                                PartSize = parts[i].Length,
                                FilePosition = parts[i].StartOffset,
                                PartNumber = i + 1
                            }
                        );
                        completedParts.Add(new PartETag(i + 1, partResponse.ETag));
                    }

                    this.s3.CompleteMultipartUpload(
                        new CompleteMultipartUploadRequest
                        {
                            BucketName = this.BucketName,
                            Key = keyName,
                            UploadId = uploadResponse.UploadId,
                            PartETags = completedParts
                        }
                    );
                }
                catch (Exception)
                {
                    this.s3.AbortMultipartUpload(
                        new AbortMultipartUploadRequest
                        {
                            BucketName = this.BucketName,
                            Key = keyName,
                            UploadId = uploadResponse.UploadId
                        }
                    );
                    throw;
                }
            }
        }

        private List<PartInfo> GetParts(long totalSize)
        {
            if (totalSize < this.PartSize * 2)
                return null;

            int wholeParts = (int)(totalSize / this.PartSize);
            var parts = new List<PartInfo>(wholeParts);

            for (int i = 0; i < wholeParts - 1; i++)
                parts.Add(new PartInfo { StartOffset = i * PartSize, Length = PartSize });

            long remainder = totalSize % this.PartSize;
            parts.Add(new PartInfo { StartOffset = (wholeParts - 1) * this.PartSize, Length = this.PartSize + remainder });

            return parts;
        }

        private struct PartInfo
        {
            public long StartOffset { get; set; }
            public long Length { get; set; }
        }
    }
}
