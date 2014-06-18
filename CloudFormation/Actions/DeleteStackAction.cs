using System.ComponentModel;
using Amazon.CloudFormation.Model;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web;

namespace Inedo.BuildMasterExtensions.Amazon.CloudFormation
{
    [ActionProperties(
        "Delete CloudFormation Stack",
        "An action that deletes an Amazon CloudFormation stack.")]
    [CustomEditor(typeof(DeleteStackActionEditor))]
    [Tag("amazon"), Tag("cloud")]
    public class DeleteStackAction : CloudFormationAction 
    {
        [Persistent]
        [DisplayName("Stack Name")]
        public string StackName { get; set; }

        [Persistent]
        [DisplayName("Wait Until Complete")]
        public bool WaitUntilComplete { get; set; }

        public override string ToString()
        {
            return string.Format("Delete Amazon CloudFormation Stack");
        }

        protected override void Execute()
        {
            this.LogInformation("Waiting for CloudFormation stack deletion.");
            if (InitClient())
            {
                LogInformation("CloudFormation delete stack {0}", StackName);
                client.DeleteStack(new DeleteStackRequest { StackName = StackName });
                if (WaitUntilComplete)
                    WaitForStack(StackName, "N/A", CloudFormationAction.DELETE_IN_PROGRESS, CloudFormationAction.DELETE_COMPLETE);
            }
            this.LogInformation("Done deleting CloudFormation stack.");
        }
    }
}
