using System.ComponentModel;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web;

namespace Inedo.BuildMasterExtensions.Amazon.CloudFormation
{
    [ActionProperties(
        "Wait For CloudFormation Stack Creation",
        "An action that waits for an Amazon CloudFormation stack to complete.")]
    [CustomEditor(typeof(WaitForStackCreationActionEditor))]
    [Tag("amazon"), Tag("cloud")]
    public class WaitForStackCreationAction : CloudFormationAction 
    {
        [Persistent]
        [DisplayName("Stack Name")]
        public string StackName { get; set; }

        public override string ToString()
        {
            return string.Format("Wait for Amazon CloudFormation Stack");
        }

        protected override void Execute()
        {
            this.LogInformation("Waiting for CloudFormation stack creation.");
            if (InitClient())
            {
                WaitForStack(StackName, "N/A", CloudFormationAction.CREATE_IN_PROGRESS, CloudFormationAction.CREATE_COMPLETE);
            }
            this.LogInformation("Done waiting for CloudFormation stack creation.");
        }

    }
}
