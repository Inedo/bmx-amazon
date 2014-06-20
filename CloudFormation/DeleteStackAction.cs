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
    public sealed class DeleteStackAction : CloudFormationActionBase 
    {
        [Persistent]
        public string StackName { get; set; }

        [Persistent]
        public bool WaitUntilComplete { get; set; }

        public override string ToString()
        {
            return string.Format("Delete Amazon CloudFormation Stack");
        }

        protected override void Execute()
        {
            this.LogInformation("Waiting for CloudFormation stack deletion.");

            using (var client = this.GetClient())
            {
                this.LogInformation("CloudFormation delete stack {0}", this.StackName);
                client.DeleteStack(new DeleteStackRequest { StackName = this.StackName });
                if (this.WaitUntilComplete)
                    this.WaitForStack(client, this.StackName, "N/A", CloudFormationActionBase.DELETE_IN_PROGRESS, CloudFormationActionBase.DELETE_COMPLETE);
            }

            this.LogInformation("Done deleting CloudFormation stack.");
        }
    }
}
