using System.ComponentModel;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Inedo.Documentation;
using Inedo.Serialization;
using Inedo.Web;

namespace Inedo.BuildMasterExtensions.Amazon.CloudFormation
{
    [DisplayName("Delete CloudFormation Stack")]
    [Description("An action that deletes an Amazon CloudFormation stack.")]
    [CustomEditor(typeof(DeleteStackActionEditor))]
    [Tag("amazon"), Tag("cloud")]
    [PersistFrom("Inedo.BuildMasterExtensions.Amazon.CloudFormation.DeleteStackAction,Amazon")]
    public sealed class DeleteStackAction : CloudFormationActionBase
    {
        [Persistent]
        public string StackName { get; set; }

        [Persistent]
        public bool WaitUntilComplete { get; set; }

        public override ExtendedRichDescription GetActionDescription()
        {
            return new ExtendedRichDescription(
                new RichDescription(
                    "Delete ",
                    new Hilite(this.StackName),
                    " CloudFormation Stack"
                ),
                new RichDescription(
                    this.WaitUntilComplete ? "and wait until the operation is complete" : string.Empty
                )
            );
        }

        protected override void Execute(IAmazonCloudFormation client)
        {
            this.LogInformation("Deleting stack {0}...", this.StackName);

            client.DeleteStack(new DeleteStackRequest { StackName = this.StackName });
            if (this.WaitUntilComplete)
            {
                if (!this.WaitForStack(client, this.StackName, "N/A", CloudFormationActionBase.DELETE_IN_PROGRESS, CloudFormationActionBase.DELETE_COMPLETE))
                    return;

                this.LogInformation("Stack deleted.");
            }
            else
            {
                this.LogInformation("Delete command issued.");
            }
        }
    }
}
