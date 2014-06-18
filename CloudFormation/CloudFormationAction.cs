using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Amazon;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;

namespace Inedo.BuildMasterExtensions.Amazon.CloudFormation
{
    public abstract class CloudFormationAction : AgentBasedActionBase, ICloudFormationAction 
    {
        public enum FailureAction
        {
            DO_NOTHING,
            ROLLBACK, 
            DELETE
        }

        public const string CREATE_IN_PROGRESS = "CREATE_IN_PROGRESS";
        public const string CREATE_FAILED = "CREATE_FAILED";
        public const string CREATE_COMPLETE = "CREATE_COMPLETE";
        public const string ROLLBACK_IN_PROGRESS = "ROLLBACK_IN_PROGRESS";
        public const string ROLLBACK_FAILED = "ROLLBACK_FAILED";
        public const string ROLLBACK_COMPLETE = "ROLLBACK_COMPLETE";
        public const string DELETE_IN_PROGRESS = "DELETE_IN_PROGRESS";
        public const string DELETE_FAILED = "DELETE_FAILED";
        public const string DELETE_COMPLETE = "DELETE_COMPLETE";
        public const string UPDATE_IN_PROGRESS = "UPDATE_IN_PROGRESS";
        public const string UPDATE_COMPLETE_CLEANUP_IN_PROGRESS = "UPDATE_COMPLETE_CLEANUP_IN_PROGRESS";
        public const string UPDATE_COMPLETE = "UPDATE_COMPLETE";
        public const string UPDATE_ROLLBACK_IN_PROGRESS = "UPDATE_ROLLBACK_IN_PROGRESS";
        public const string UPDATE_ROLLBACK_FAILED = "UPDATE_ROLLBACK_FAILED";
        public const string UPDATE_ROLLBACK_COMPLETE_CLEANUP_IN_PROGRESS = "UPDATE_ROLLBACK_COMPLETE_CLEANUP_IN_PROGRESS";
        public const string UPDATE_ROLLBACK_COMPLETE = "UPDATE_ROLLBACK_COMPLETE";

        [Persistent]
        [DisplayName("Access Key")]
        public string AccessKey { get; set; }

        [Persistent]
        [DisplayName("Secret Key")]
        public string SecretKey { get; set; }

        [Persistent]
        [DisplayName("Encrypt Keys in database")]
        public bool EncryptKeys { get; set; }

        [Persistent]
        public string Region { get; set; }

        public string AccessKeyCleartext
        {
            get
            {
                string local = EncryptKeys ? AccessKey.Decrypt() : AccessKey;
                if (string.IsNullOrEmpty(local) && !string.IsNullOrEmpty(GlobalAccessKey()))
                    return GlobalAccessKey();
                return local;
            }

            set
            {
                if (!this.EncryptKeys)
                    this.AccessKey = value;
                else
                {
                    this.AccessKey = value.Encrypt(100); ;
                }
            }
        }

        public string SecretKeyCleartext
        {
            get
            {
                string local = EncryptKeys ? SecretKey.Decrypt() : SecretKey;
                if (string.IsNullOrEmpty(local) && !string.IsNullOrEmpty(GlobalSecretKey()))
                    return GlobalSecretKey();
                return local;
            }

            set
            {
                if (!this.EncryptKeys)
                    this.SecretKey = value;
                else
                {
                    this.SecretKey = value.Encrypt(100); ;
                }
            }
        }

        private string GlobalAccessKey()
        {
            try
            {
                var conf = base.GetExtensionConfigurer() as Inedo.BuildMasterExtensions.Amazon.AmazonConfigurer;
                if (null != conf && !string.IsNullOrEmpty(conf.AccessKeyId))
                    return conf.AccessKeyId;
            }
            catch (Exception)
            { }
            return null;
        }

        private string GlobalSecretKey()
        {
            try
            {
                var conf = base.GetExtensionConfigurer() as Inedo.BuildMasterExtensions.Amazon.AmazonConfigurer;
                if (null != conf && !string.IsNullOrEmpty(conf.SecretAccessKey))
                    return conf.SecretAccessKey;
            }
            catch (Exception)
            { }
            return null;
        }

        internal IAmazonCloudFormation client;

        internal bool InitClient()
        {
            try
            {
                var endpoint = string.IsNullOrEmpty(this.Region) ? RegionEndpoint.USEast1  :RegionEndpoint.GetBySystemName(this.Region);
                client = AWSClientFactory.CreateAmazonCloudFormationClient(this.AccessKeyCleartext, this.SecretKeyCleartext,endpoint);
                return null != client;
            }
            catch (Exception ex)
            {
                LogError("CloudFormation error initializing AWS client: {0}", ex.ToString());
            }
            return false;
        }

        internal string CreateStack(string Capabilities, string Parameters, string Tags, string StackName, string TemplateData, string BucketName, string OnFailure)
        {
            try
            {
                var req = new CreateStackRequest();
                req.Capabilities = (List<string>)GetCapabilities(Capabilities);
                req.OnFailure = OnFailure; // "ROLLBACK";
                req.Parameters = (List<Parameter>)GetParameters(Parameters);
                req.Tags = (List<Tag>)GetTags(Tags);
                req.StackName = StackName;
                
                if (string.IsNullOrEmpty(BucketName))
                    req.TemplateBody = TemplateData;
                else
                    req.TemplateURL = BucketName;
                var result = client.CreateStack(req);
                return result.StackId;
            }
            catch (Exception ex)
            {
                LogError("CloudFormation error creating stack {0}:{1}", StackName, ex.ToString());
            }
            return string.Empty;
        }

        internal void WaitForStack(string StackName, string StackID, string StatusToWaitOn, string SuccessStatus)
        {
            LogInformation("CloudFormation waiting for stack {0} ({1}) to get out of {2} status.", StackName, StackID, StatusToWaitOn);
            var req = new DescribeStacksRequest();
            req.StackName = StackName;
            var resp = client.DescribeStacks(req);
            if (resp.Stacks.Count < 1)
            {
                LogError("CloudFormation error in WaitForStack: No stacks in DescribeStacks");
                return;
            }
            while (resp.Stacks[0].StackStatus == StatusToWaitOn)
            {
                System.Threading.Thread.Sleep(20000);
                resp = client.DescribeStacks(req);
                if (resp.Stacks.Count < 1)
                {
                    LogError("CloudFormation error in WaitForStack: No stacks in DescribeStacks");
                    return;
                }
            }
            if (SuccessStatus == resp.Stacks[0].StackStatus)
                LogInformation("CloudFormation stack {0} ({1}) completed successfully. Output: {2}", StackName, StackID, resp.Stacks[0].Outputs.AsString());
            else
                LogError("CloudFormation stack {0} ({1}) failed with status: {2}. Output: {3}", StackName, StackID, resp.Stacks[0].StackStatus, resp.Stacks[0].Outputs.AsString());
        }

        
        internal IList<Parameter> GetParameters(string Parameters)
        {
            return (from p in Parameters.ParseNameValue() select new Parameter { ParameterKey = p.Key, ParameterValue = p.Value }).ToList<Parameter>();
        }

        internal IList<Tag> GetTags(string Tags)
        {
            return (from t in Tags.ParseNameValue() select new Tag { Key = t.Key, Value = t.Value }).Take(10).ToList<Tag>();
        }

        internal IList<string> GetCapabilities(string Capabilities)
        {
            return (from s in Capabilities.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries) select s).ToList<string>();
        }

    }
}
