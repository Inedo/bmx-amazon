using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Data;
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

        public override ActionDescription GetActionDescription()
        {
            var longDesc = new LongActionDescription();
            if (!string.IsNullOrEmpty(this.BucketName))
                longDesc.AppendContent("from S3 URL: ", new Hilite(this.BucketName));
            else if (!string.IsNullOrEmpty(this.TemplateFile))
                longDesc.AppendContent("from ", new Hilite(this.TemplateFile), " configuration file");
            else if (!string.IsNullOrEmpty(this.TemplateText))
                longDesc.AppendContent("from ", new Hilite("template stored in action"));

            return new ActionDescription(
                new ShortActionDescription(
                    "Deploy CloudFormation Template for Stack ",
                    new Hilite(this.StackName)
                ),
                longDesc
            );
        }

        protected override void Execute(IAmazonCloudFormation client)
        {
            this.LogInformation("CloudFormation starting template deploy.");

            var templateData = string.Empty;

            if (string.IsNullOrEmpty(this.BucketName))
                templateData = this.GetTemplateText();

            if (string.IsNullOrEmpty(this.BucketName) && string.IsNullOrEmpty(templateData))
            {
                this.LogError("Deployment requires either an S3 bucket, template config file, or template data specified in the action.");
                return;
            }

            var stackId = this.CreateStack(client, templateData);
            if (string.IsNullOrEmpty(stackId))
                return;

            this.LogInformation("Successfully launched stack {0} (ID: {1}).", this.StackName, stackId);

            if (this.WaitUntilComplete)
            {
                this.LogInformation("Waiting for stack...");

                if (!this.WaitForStack(client, this.StackName, stackId, CloudFormationActionBase.CREATE_IN_PROGRESS, CloudFormationActionBase.CREATE_COMPLETE))
                    return;

                this.LogInformation("Stack created.");
            }
        }

        private string CreateStack(IAmazonCloudFormation client, string templateData)
        {
            var req = new CreateStackRequest
            {
                Capabilities = (this.Capabilities ?? string.Empty)
                    .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                    .ToList(),
                Parameters = ParseNameValue(this.Parameters)
                    .Select(p => new Parameter { ParameterKey = p.Key, ParameterValue = p.Value })
                    .ToList(),
                Tags = ParseNameValue(this.Tags)
                    .Select(t => new Tag { Key = t.Key, Value = t.Value })
                    .Take(10)
                    .ToList(),
                OnFailure = this.ActionOnFail.ToString(),
                StackName = this.StackName
            };

            if (string.IsNullOrEmpty(this.BucketName))
                req.TemplateBody = templateData;
            else
                req.TemplateURL = this.BucketName;

            try
            {
                var result = client.CreateStack(req);
                return result.StackId;
            }
            catch(Exception ex)
            {
                this.LogError("Error creating stack: " + ex.Message);
                return null;
            }
        }

        private string GetTemplateText()
        {
            if (!string.IsNullOrEmpty(this.TemplateText))
                return this.TemplateText;

            this.LogInformation("CloudFormation TemplateText is empty, looking for a config file");
            if (!string.IsNullOrEmpty(this.TemplateFile))
            {
                var config = StoredProcs.ConfigurationFiles_GetConfigurationFiles(this.Context.ApplicationId, this.Context.DeployableId, Domains.YN.No)
                    .Execute()
                    .FirstOrDefault(c => string.Equals(c.FilePath_Text, this.TemplateFile, StringComparison.OrdinalIgnoreCase));

                if (config == null)
                {
                    this.LogError("Unable to find configuration file {0}.", this.TemplateFile);
                    return null;
                }

                var configVersion = StoredProcs.ConfigurationFiles_GetConfigurationFileVersion(
                    ConfigurationFile_Id: config.ConfigurationFile_Id,
                    Release_Number: this.Context.ReleaseNumber)
                    .Execute()
                    .FirstOrDefault(v => v.Environment_Id == this.Context.EnvironmentId);

                if (configVersion == null)
                {
                    var environment = StoredProcs.Environments_GetEnvironment(this.Context.EnvironmentId)
                        .Execute()
                        .Environments
                        .FirstOrDefault();

                    this.LogError("Unable to find instance of configuration file {0} associated with {1} environment.", this.TemplateFile, environment != null ? environment.Environment_Name : "unknown");
                    return null;
                }

                if (configVersion.File_Bytes == null || configVersion.File_Bytes.Length == 0)
                {
                    this.LogError("Configuration file {0} is empty.", this.TemplateFile);
                    return null;
                }

                return Encoding.UTF8.GetString(configVersion.File_Bytes);
            }

            return null;
        }

        private static Dictionary<string, string> ParseNameValue(string value)
        {
            return (value ?? string.Empty)
                .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                .Select(v => v.Split(new[] { '=' }, 2))
                .Where(v => v.Length == 2)
                .GroupBy(v => v[0])
                .ToDictionary(v => v.Key, v => v.First()[1]);
        }

        public enum FailureAction
        {
            DO_NOTHING,
            ROLLBACK,
            DELETE
        }
    }
}
