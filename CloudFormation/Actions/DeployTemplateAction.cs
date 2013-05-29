using System;
using System.Text;
using System.ComponentModel;
using System.Linq;

using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web;

namespace Inedo.BuildMasterExtensions.Amazon.CloudFormation
{
    [ActionProperties(
        "Deploy CloudFormation Template",
        "An action that deploys an Amazon CloudFormation template.",
        "Amazon")]
    [CustomEditor(typeof(DeployTemplateActionEditor))]
    public sealed class DeployTemplateAction : CloudFormationAction 
    {
        [Persistent]
        [DisplayName("S3 Bucket")]
        public string BucketName { get; set; }

        [Persistent]
        [DisplayName("Template Configuration File")]
        public string TemplateFile { get; set; }

        [Persistent]
        [DisplayName("Template Parameters")]
        public string Parameters { get; set; }

        [Persistent]
        [DisplayName("Template Text")]
        public string TemplateText { get; set; }

        [Persistent]
        [DisplayName("Stack Name")]
        public string StackName { get; set; }

        [Persistent]
        [DisplayName("Tags")]
        public string Tags { get; set; }

        [Persistent]
        [DisplayName("Capabilities")]
        public string Capabilities { get; set; }

        [Persistent]
        [DisplayName("Wait Until Complete")]
        public bool WaitUntilComplete { get; set; }

        [Persistent]
        [DisplayName("Action to take on failure")]
        public FailureAction ActionOnFail { get; set; }

        internal string templateData { get; set; }

        public override string ToString()
        {
            return string.Format("Deploy Amazon CloudFormation template.");
        }

        protected override void Execute()
        {
            this.LogInformation("CloudFormation starting template deploy.");
            if (InitClient())
            {
                DeployIt();
            }
            this.LogInformation("CloudFormation finished template deploy");
        }


        private void DeployIt()
        {
            if (string.IsNullOrEmpty(BucketName))
                templateData = GetTemplateText();
            if(string.IsNullOrEmpty(BucketName) && string.IsNullOrEmpty(templateData))
            {
                LogError("CloudFormation deployment requires either an S3 bucket or a valid template config file or valid template data");
                return;
            }
            var stackResult = CreateStack(Capabilities, Parameters, Tags, StackName, templateData, BucketName, ActionOnFail.ToString());
            if (string.IsNullOrEmpty(stackResult))
            {
                LogError("CloudFormation could not deploy stack {0}", StackName);
            } else {
                LogInformation("CloudFormation successfully launched {0} with an ID of {1}", StackName, stackResult);
            }
            if (!string.IsNullOrEmpty(stackResult) && WaitUntilComplete)
                WaitForStack(this.StackName,stackResult,CloudFormationAction.CREATE_IN_PROGRESS, CloudFormationAction.CREATE_COMPLETE);
        }


        private string GetTemplateText()
        {
            try
            {
                if (!string.IsNullOrEmpty(this.TemplateText))
                    return this.TemplateText.Substitute(Context.Variables);
                LogInformation("CloudFormation TemplateText is empty, looking for a config file");
                if (!string.IsNullOrEmpty(this.TemplateFile))
                {
                    var config = (from r in Inedo.BuildMaster.Data.StoredProcs.ConfigurationFiles_GetConfigurationFiles(this.Context.ApplicationId, this.Context.DeployableId, "N").Execute() where r.FilePath_Text.Trim().ToLowerInvariant() == this.TemplateFile.Trim().ToLowerInvariant() select r).FirstOrDefault();
                    if (null == config)
                    {
                        LogError("Cloudformation unable to GetConfigurationFiles {0}, {1}, N for FilePath_Text = \"{2}\"", this.Context.ApplicationId, this.Context.DeployableId, this.TemplateFile);
                    } else {
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
                            return Encoding.Default.GetString(file.File_Bytes).Substitute(Context.Variables);
                        }
                    }
                }
                this.LogError("CloudFormation Deploy using a template requires either a template configuration file or template data.");
                return null;            
            }
            catch(Exception ex)
            {
                this.LogError("CloudFormation Error in GetTemplateText: {0}",ex.ToString());
            }
            return null;
        }

    }
}
