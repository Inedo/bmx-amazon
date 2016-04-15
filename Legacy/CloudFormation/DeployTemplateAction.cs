using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Inedo.BuildMaster.ConfigurationFiles;
using Inedo.BuildMaster.Data;
using Inedo.BuildMaster.Web;
using Inedo.Documentation;
using Inedo.Serialization;

namespace Inedo.BuildMasterExtensions.Amazon.CloudFormation
{
    [DisplayName("Deploy CloudFormation Template")]
    [Description("An action that deploys an Amazon CloudFormation template.")]
    [CustomEditor(typeof(DeployTemplateActionEditor))]
    [Tag("amazon"), Tag("cloud")]
    public sealed class DeployTemplateAction : CloudFormationActionBase
    {
        [Persistent]
        public string BucketName { get; set; }
        [Persistent]
        public string TemplateFile { get; set; }
        [Persistent]
        public string TemplateInstanceName { get; set; }
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

        public override ExtendedRichDescription GetActionDescription()
        {
            var longDesc = new RichDescription();
            if (!string.IsNullOrEmpty(this.BucketName))
                longDesc.AppendContent("from S3 URL: ", new Hilite(this.BucketName));
            else if (!string.IsNullOrEmpty(this.TemplateFile))
                longDesc.AppendContent("from ", new Hilite(this.TemplateFile), " configuration file");
            else if (!string.IsNullOrEmpty(this.TemplateText))
                longDesc.AppendContent("from ", new Hilite("template stored in action"));

            return new ExtendedRichDescription(
                new RichDescription(
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
            catch (Exception ex)
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
                var configInfo = DB.ConfigurationFiles_GetConfigurationFiles(Application_Id: this.Context.ApplicationId, Deployable_Id: null, IncludeInstances_Indicator: Domains.YN.Yes);

                var configFiles = from file in configInfo.ConfigurationFiles_Extended
                                  let matchesDeployable = file.Deployable_Id == this.Context.DeployableId
                                  let matchesName = string.Equals(file.ConfigurationFile_Name, this.TemplateFile, StringComparison.OrdinalIgnoreCase)
                                  let matchesPath = string.Equals(file.FilePath_Text, this.TemplateFile, StringComparison.OrdinalIgnoreCase)
                                  where matchesName || matchesPath
                                  orderby matchesDeployable descending, matchesName descending, matchesPath descending
                                  select file;

                var configFile = configFiles.FirstOrDefault();

                if (configFile == null)
                {
                    this.LogError("Unable to find configuration file {0}.", this.TemplateFile);
                    return null;
                }

                string instanceName = this.GetTemplateFileInstanceName(configInfo.ConfigurationFileInstances_Extended);

                if (instanceName == null)
                    return null;

                var writer = new StringWriter();
                var deployer = new ConfigurationFileDeployer(
                    new ConfigurationFileDeploymentOptions
                    {
                        ConfigurationFileId = configFile.ConfigurationFile_Id,
                        InstanceName = instanceName
                    }
                );
                deployer.Write((Inedo.BuildMaster.Extensibility.IGenericBuildMasterContext)this.Context, writer);

                string configFileContents = writer.ToString();
                if (string.IsNullOrEmpty(configFileContents))
                {
                    this.LogError("Configuration file {0} is empty.", this.TemplateFile);
                    return null;
                }

                return configFileContents;
            }

            return null;
        }

        private string GetTemplateFileInstanceName(IEnumerable<Tables.ConfigurationFileInstances_Extended> instances)
        {
            if (!string.IsNullOrEmpty(this.TemplateInstanceName))
                return this.TemplateInstanceName;

            var instance = instances.FirstOrDefault(i => i.Environment_Id == this.Context.EnvironmentId);
            if (instance == null)
            {
                this.LogError("Unable to find configuration file instance based on the current environment. "
                    + "Make sure to specify a template file instance name for this action.");
                return null;
            }

            return instance.Instance_Name;
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
