using System.ComponentModel;
using Amazon.CloudFormation;
using Inedo.BuildMaster.Web;
using Inedo.Documentation;
using Inedo.Serialization;

namespace Inedo.BuildMasterExtensions.Amazon.CloudFormation
{
    [DisplayName("Wait For CloudFormation Stack Creation")]
    [Description("An action that waits for an Amazon CloudFormation stack to complete.")]
    [CustomEditor(typeof(WaitForStackCreationActionEditor))]
    [Tag("amazon"), Tag("cloud")]
    public sealed class WaitForStackCreationAction : CloudFormationActionBase 
    {
        [Persistent]
        public string StackName { get; set; }

        public override ExtendedRichDescription GetActionDescription()
        {
            return new ExtendedRichDescription(
                new RichDescription(
                    "Wait for ",
                    new Hilite(this.StackName),
                    " CloudFormation Stack to be Created"
                )
            );
        }

        protected override void Execute(IAmazonCloudFormation client)
        {
            this.LogInformation("Waiting for {0} stack to be created...", this.StackName);

            if (!this.WaitForStack(client, this.StackName, "N/A", CloudFormationActionBase.CREATE_IN_PROGRESS, CloudFormationActionBase.CREATE_COMPLETE))
                return;

            this.LogInformation("Stack is created.");
        }
    }
}
