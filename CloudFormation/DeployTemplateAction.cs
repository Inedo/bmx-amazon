using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web;

namespace Inedo.BuildMasterExtensions.Amazon.CloudFormation
{
    [ActionProperties(
        "Deploy CloudFormation Template",
        "An action that deploys an Amazon CloudFormation template.")]
    [CustomEditor(typeof(DeployTemplateActionEditor))]
    [Tag("amazon"), Tag("cloud")]
    public sealed class DeployTemplateAction : CloudFormationActionBase 
    {
        [Persistent]
        public string BucketName { get; set; }

        [Persistent]
        public string TemplateFile { get; set; }

        [Persistent]
        public string Parameters { get; set; }

        [Persistent]
        public string TemplateText { get; set; }

        [Persistent]
        public string StackName { get; set; }

        [Persistent]
        public string Tags { get; set; }

        [Persistent]
        public string Capabilities { get; set; }

        [Persistent]
        public bool WaitUntilComplete { get; set; }

        [Persistent]
        public FailureAction ActionOnFail { get; set; }

        internal string templateData { get; set; }

        public override string ToString()
        {
            return string.Format("Deploy Amazon CloudFormation template.");
        }

        protected override void Execute()
        {
            this.LogInformation("CloudFormation starting template deploy.");
            using (var client = this.GetClient())
            {
                if (string.IsNullOrEmpty(this.BucketName))
                    templateData = this.GetTemplateText();

                if (string.IsNullOrEmpty(this.BucketName) && string.IsNullOrEmpty(templateData))
                {
                    this.LogError("CloudFormation deployment requires either an S3 bucket or a valid template config file or valid template data");
                    return;
                }

                var stackResult = CreateStack(client, this.Capabilities, this.Parameters, this.Tags, this.StackName, templateData, this.BucketName, this.ActionOnFail.ToString());
                if (string.IsNullOrEmpty(stackResult))
                {
                    this.LogError("CloudFormation could not deploy stack {0}", this.StackName);
                }
                else
                {
                    this.LogInformation("CloudFormation successfully launched {0} with an ID of {1}", this.StackName, stackResult);
                }

                if (!string.IsNullOrEmpty(stackResult) && this.WaitUntilComplete)
                    this.WaitForStack(client, this.StackName, stackResult, CloudFormationActionBase.CREATE_IN_PROGRESS, CloudFormationActionBase.CREATE_COMPLETE);
            }

            this.LogInformation("CloudFormation finished template deploy");
        }

        private string CreateStack(IAmazonCloudFormation client, string capabilities, string parameters, string tags, string stackName, string templateData, string bucketName, string onFailure)
        {
            try
            {
                var req = new CreateStackRequest
                {
                    Capabilities = GetCapabilities(capabilities),
                    OnFailure = onFailure, // "ROLLBACK";
                    Parameters = GetParameters(parameters),
                    Tags = GetTags(tags),
                    StackName = stackName
                };

                if (string.IsNullOrEmpty(BucketName))
                    req.TemplateBody = templateData;
                else
                    req.TemplateURL = bucketName;

                var result = client.CreateStack(req);
                return result.StackId;
            }
            catch (Exception ex)
            {
                this.LogError("CloudFormation error creating stack {0}:{1}", this.StackName, ex);
            }

            return string.Empty;
        }


        private string GetTemplateText()
        {
            try
            {
                if (!string.IsNullOrEmpty(this.TemplateText))
                    return Substitute(this.TemplateText, this.Context.Variables);

                this.LogInformation("CloudFormation TemplateText is empty, looking for a config file");
                if (!string.IsNullOrEmpty(this.TemplateFile))
                {
                    var config = (from r in Inedo.BuildMaster.Data.StoredProcs.ConfigurationFiles_GetConfigurationFiles(this.Context.ApplicationId, this.Context.DeployableId, "N").Execute() where r.FilePath_Text.Trim().ToLowerInvariant() == this.TemplateFile.Trim().ToLowerInvariant() select r).FirstOrDefault();
                    if (null == config)
                    {
                        this.LogError("Cloudformation unable to GetConfigurationFiles {0}, {1}, N for FilePath_Text = \"{2}\"", this.Context.ApplicationId, this.Context.DeployableId, this.TemplateFile);
                    }
                    else
                    {
                        // get the latest config file version
                        var file = (from v in Inedo.BuildMaster.Data.StoredProcs.ConfigurationFiles_GetConfigurationFileVersions(config.ConfigurationFile_Id, this.Context.ApplicationId, null, null, null, 1).Execute() select v).FirstOrDefault();
                        if (null == file)
                        {
                            this.LogError("CloudFormation is unable to find an active template configuration file for the application id: {0} and deployable id: {1}",this.Context.ApplicationId, this.Context.DeployableId);
                            return null;
                        } else {
                            if (0 == file.File_Bytes.Length)
                            {
                                this.LogError("CloudFormation template file for {0} is empty.",this.TemplateFile);
                                return null;
                            }
                            LogInformation("CloudFormation returning template config file.");
                            return Substitute(Encoding.Default.GetString(file.File_Bytes), Context.Variables);
                        }
                    }
                }

                this.LogError("CloudFormation Deploy using a template requires either a template configuration file or template data.");
                return null;            
            }
            catch(Exception ex)
            {
                this.LogError("CloudFormation Error in GetTemplateText: {0}", ex.ToString());
            }

            return null;
        }

        private static string Substitute(string value, IDictionary<string, string> variables)
        {
            var retVal = new StringBuilder(value);
            foreach (var v in variables)
                retVal.Replace(String.Format("%{0}%", v.Key), v.Value);

            return retVal.ToString();
        }

        private List<Parameter> GetParameters(string parameters)
        {
            return ParseNameValue(parameters)
                .Select(p => new Parameter { ParameterKey = p.Key, ParameterValue = p.Value })
                .ToList();
        }

        private List<Tag> GetTags(string tags)
        {
            return ParseNameValue(tags)
                .Select(t => new Tag { Key = t.Key, Value = t.Value })
                .Take(10)
                .ToList();
        }

        private List<string> GetCapabilities(string capabilities)
        {
            return capabilities
                .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                .ToList();
        }

        private static IDictionary<string, string> ParseNameValue(string value)
        {
            var retVal = new Dictionary<string, string>();
            var start = value.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var s in start)
            {
                var result = s.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                if (result.Length > 1)
                    retVal.Add(result[0], result[1]);
            }

            return retVal;
        }

        public enum FailureAction
        {
            DO_NOTHING,
            ROLLBACK,
            DELETE
        }
    }
}
