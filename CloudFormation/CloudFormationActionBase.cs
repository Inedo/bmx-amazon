using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Amazon;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Actions;

namespace Inedo.BuildMasterExtensions.Amazon.CloudFormation
{
    public abstract class CloudFormationActionBase : ActionBase, IMissingPersistentPropertyHandler
    {
        private string overriddenAccessKey;
        private string overriddenSecretKey;
        private string overriddenRegion;

        internal CloudFormationActionBase()
        {
        }

        public const string CREATE_IN_PROGRESS = "CREATE_IN_PROGRESS";
        public const string CREATE_FAILED = "CREATE_FAILED";
        public const string CREATE_COMPLETE = "CREATE_COMPLETE";
        public const string DELETE_IN_PROGRESS = "DELETE_IN_PROGRESS";
        public const string DELETE_FAILED = "DELETE_FAILED";
        public const string DELETE_COMPLETE = "DELETE_COMPLETE";

        protected IAmazonCloudFormation GetClient()
        {
            var configurer = (AmazonConfigurer)this.GetExtensionConfigurer();

            var accessKey = this.overriddenAccessKey ?? configurer.AccessKeyId;
            var secretKey = this.overriddenSecretKey ?? configurer.SecretAccessKey;
            var endpointName = this.overriddenRegion ?? configurer.RegionEndpoint;

            if (string.IsNullOrEmpty(accessKey))
            {
                this.LogError("Amazon AWS access key must be specified in the configuration profile for the Amazon extension.");
                return null;
            }

            if (string.IsNullOrEmpty(secretKey))
            {
                this.LogError("Amazon AWS secret key must be specified in the configuration profile for the Amazon extension.");
                return null;
            }

            if (string.IsNullOrEmpty(endpointName))
                endpointName = RegionEndpoint.USWest1.SystemName;

            try
            {
                return AWSClientFactory.CreateAmazonCloudFormationClient(
                    accessKey,
                    secretKey,
                    RegionEndpoint.GetBySystemName(endpointName)
                );
            }
            catch (Exception ex)
            {
                this.LogError("Cannot connect to AWS endpoint: " + ex.Message);
                return null;
            }
        }

        protected void WaitForStack(IAmazonCloudFormation client, string stackName, string stackId, string statusToWaitOn, string successStatus)
        {
            this.LogInformation("CloudFormation waiting for stack {0} ({1}) to get out of {2} status.", stackName, stackId, statusToWaitOn);
            var req = new DescribeStacksRequest { StackName = stackName };

            var resp = client.DescribeStacks(req);
            if (resp.Stacks.Count < 1)
            {
                this.LogError("CloudFormation error in WaitForStack: No stacks in DescribeStacks");
                return;
            }

            while (resp.Stacks[0].StackStatus == statusToWaitOn)
            {
                Thread.Sleep(20000);
                resp = client.DescribeStacks(req);
                if (resp.Stacks.Count < 1)
                {
                    this.LogError("CloudFormation error in WaitForStack: No stacks in DescribeStacks");
                    return;
                }
            }

            if (successStatus == resp.Stacks[0].StackStatus)
                this.LogInformation("CloudFormation stack {0} ({1}) completed successfully. Output: {2}", stackName, stackId, AsString(resp.Stacks[0].Outputs));
            else
                this.LogError("CloudFormation stack {0} ({1}) failed with status: {2}. Output: {3}", stackName, stackId, resp.Stacks[0].StackStatus, AsString(resp.Stacks[0].Outputs));
        }
        
        void IMissingPersistentPropertyHandler.OnDeserializedMissingProperties(IDictionary<string, string> missingProperties)
        {
            var accessKey = missingProperties.GetValueOrDefault("AccessKey");
            var secretKey = missingProperties.GetValueOrDefault("SecretKey");
            var region = missingProperties.GetValueOrDefault("Region");
            bool encryptKeys;
            bool.TryParse(missingProperties.GetValueOrDefault("EncryptKeys") ?? bool.FalseString, out encryptKeys);

            if (!string.IsNullOrEmpty(accessKey))
            {
                if (encryptKeys)
                    accessKey = DecryptKey(accessKey);

                this.overriddenAccessKey = accessKey;
            }

            if (!string.IsNullOrEmpty(secretKey))
            {
                if (encryptKeys)
                    secretKey = DecryptKey(secretKey);

                this.overriddenSecretKey = secretKey;
            }

            if (!string.IsNullOrEmpty(region))
                this.overriddenRegion = region;
        }

        private static string DecryptKey(string value)
        {
            if (!value.Contains('^'))
            {
                return UnicodeEncoding.Unicode.GetString(ProtectedData.Unprotect(Convert.FromBase64String(value), new byte[0], DataProtectionScope.LocalMachine));
            }
            else
            {
                var val = value.Substring(value.IndexOf('^') + 1);
                var ent = value.Substring(0, value.IndexOf('^'));
                return UnicodeEncoding.Unicode.GetString(ProtectedData.Unprotect(Convert.FromBase64String(val), Convert.FromBase64String(ent), DataProtectionScope.LocalMachine));
            }
        }

        private static string AsString(List<Output> value)
        {
            var retVal = new StringBuilder();
            foreach (var o in value)
                retVal.AppendFormat("{{{0} / {1}}}\r\n", o.OutputKey, o.OutputValue);

            return retVal.ToString();
        }
    }
}
