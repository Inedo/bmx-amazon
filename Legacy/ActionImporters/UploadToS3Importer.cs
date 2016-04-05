using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.BuildMasterExtensions.Amazon.Operations.S3;
using Inedo.BuildMasterExtensions.Amazon.S3;

namespace Inedo.BuildMasterExtensions.Amazon.Legacy.ActionImporters
{
    internal sealed class UploadToS3Importer : IActionOperationConverter<UploadFilesToS3Action, UploadFilesToS3Operation>
    {
        public ConvertedOperation<UploadFilesToS3Operation> ConvertActionToOperation(UploadFilesToS3Action action, IActionConverterContext context)
        {
            var mask = context.ConvertLegacyMask(action.FileMasks, action.Recursive);
            var configurer = (AmazonConfigurer)context.Configurer;

            return new UploadFilesToS3Operation
            {
                Includes = mask.Includes,
                Excludes = mask.Excludes,
                SourceDirectory = context.ConvertLegacyExpression(action.OverriddenSourceDirectory),
                KeyPrefix = context.ConvertLegacyExpression(action.KeyPrefix),
                BucketName = context.ConvertLegacyExpression(action.BucketName),
                ReducedRedundancy = action.ReducedRedundancy,
                MakePublic = action.MakePublic,
                Encrypted = action.Encrypted,
                AccessKey = configurer.AccessKeyId,
                SecretAccessKey = configurer.SecretAccessKey,
                PartSize = configurer.S3PartSize * 1024 * 1024,
                RegionEndpoint = configurer.RegionEndpoint
            };
        }
    }
}
