using System;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web;

namespace Inedo.BuildMasterExtensions.Amazon.EC2
{
    [ActionProperties(
        "Create Amazon EC2 Instance",
        "Launches an Amazon EC2 instance using the specified AMI.")]
    [CustomEditor(typeof(CreateEC2InstanceActionEditor))]
    [Tag("amazon"), Tag("cloud")]
    public sealed class CreateEC2InstanceAction : RemoteActionBase
    {
        [Persistent]
        public string AmiID { get; set; }
        [Persistent]
        public string IPAddress { get; set; }

        public override ActionDescription GetActionDescription()
        {
            return new ActionDescription(
                new ShortActionDescription(
                    "Create EC2 Instance from ",
                    new Hilite(this.AmiID)
                ),
                new LongActionDescription(
                    !string.IsNullOrEmpty(this.IPAddress) ? ("with public IP address: " + this.IPAddress) : string.Empty
                )
            );
        }

        protected override void Execute()
        {
            if (string.IsNullOrEmpty(this.AmiID))
                throw new InvalidOperationException("Image ID is required.");

            System.Net.IPAddress address;
            if (!string.IsNullOrEmpty(this.IPAddress) && !System.Net.IPAddress.TryParse(this.IPAddress, out address))
                throw new InvalidOperationException("Invalid IP address.");

            var cfg = (AmazonConfigurer)GetExtensionConfigurer();
            if (string.IsNullOrEmpty(cfg.AccessKeyId) || string.IsNullOrEmpty(cfg.SecretAccessKey))
                throw new InvalidOperationException("A valid Amazon access key ID and secret access key pair must be specified in the EC2 extension configuration.");

            ExecuteRemoteCommand("exec");
        }
        protected override string ProcessRemoteCommand(string name, string[] args)
        {
            LogInformation("Contacting Amazon EC2 Service...");

            var cfg = (AmazonConfigurer)GetExtensionConfigurer();
            var ec2 = global::Amazon.AWSClientFactory.CreateAmazonEC2Client(cfg.AccessKeyId, cfg.SecretAccessKey);

            LogInformation(string.Format("Creating instance from {0}", this.AmiID));

            var response = ec2.RunInstances(new global::Amazon.EC2.Model.RunInstancesRequest()
            {
                ImageId = AmiID,
                MinCount = 1,
                MaxCount = 1
            });

            var instanceId = response.Reservation.Instances[0].InstanceId;

            LogInformation(string.Format("Instance created (ID: {0})", instanceId));

            if (!string.IsNullOrEmpty(this.IPAddress))
            {
                LogInformation(string.Format("Associating instance {0} with IP address {1}", instanceId, this.IPAddress));

                ec2.AssociateAddress(new global::Amazon.EC2.Model.AssociateAddressRequest()
                {
                    InstanceId = instanceId,
                    PublicIp = this.IPAddress
                });

                LogInformation("IP address associated.");
            }

            return string.Empty;
        }
    }
}
