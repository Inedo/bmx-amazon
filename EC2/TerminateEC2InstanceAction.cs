using System;
using System.Collections.Generic;
using System.ComponentModel;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.Documentation;
using Inedo.Serialization;
using Inedo.Web;

namespace Inedo.BuildMasterExtensions.Amazon.EC2
{
    [DisplayName("Terminate Amazon EC2 Instance")]
    [Description("Terminates an Amazon EC2 instance at the specified IP Address.")]
    [Tag("amazon"), Tag("cloud")]
    [CustomEditor(typeof(TerminateEC2InstanceActionEditor))]
    [PersistFrom("Inedo.BuildMasterExtensions.Amazon.EC2.TerminateEC2InstanceAction,Amazon")]
    public sealed class TerminateEC2InstanceAction : ActionBase
    {
        [Persistent]
        public string InstanceIdOrIPAddress { get; set; }

        public override ExtendedRichDescription GetActionDescription()
        {
            return new ExtendedRichDescription(
                new RichDescription(
                    "Terminate ",
                    new Hilite(this.InstanceIdOrIPAddress),
                    " EC2 Instance"
                )
            );
        }

        protected override void Execute()
        {
            if (string.IsNullOrEmpty(this.InstanceIdOrIPAddress))
                throw new InvalidOperationException("Instance ID is not specified.");

            var cfg = (AmazonConfigurer)GetExtensionConfigurer();
            if (string.IsNullOrEmpty(cfg.AccessKeyId) || string.IsNullOrEmpty(cfg.SecretAccessKey))
                throw new InvalidOperationException("A valid Amazon access key ID and secret access key pair must be specified in the EC2 extension configuration.");

            LogInformation("Contacting Amazon EC2 Service...");

            var instanceId = this.InstanceIdOrIPAddress;

            var ec2 = new global::Amazon.EC2.AmazonEC2Client(cfg.AccessKeyId, cfg.SecretAccessKey);

            System.Net.IPAddress address;
            if (System.Net.IPAddress.TryParse(instanceId, out address))
            {
                LogInformation(string.Format("Looking up instance ID for {0}", instanceId));

                var addrResult = ec2.DescribeAddresses(new global::Amazon.EC2.Model.DescribeAddressesRequest()
                {
                    PublicIps = new List<string>()
                    {
                        instanceId
                    }
                });

                if (addrResult.Addresses.Count == 0)
                    throw new InvalidOperationException(string.Format("IP address {0} is not associated with an instance.", instanceId));

                var addressInfo = addrResult.Addresses[0];
                instanceId = addressInfo.InstanceId;

                LogInformation("Disassociating IP address");
                ec2.DisassociateAddress(new global::Amazon.EC2.Model.DisassociateAddressRequest() { PublicIp = this.InstanceIdOrIPAddress });
            }

            LogInformation(string.Format("Terminating instance {0}", instanceId));

            var response = ec2.TerminateInstances(new global::Amazon.EC2.Model.TerminateInstancesRequest()
            {
                InstanceIds = new List<string>()
                {
                    instanceId
                }
            });

            if (response.TerminatingInstances.Count > 0)
                LogInformation(string.Format("Instance {0} has been terminated.", instanceId));
            else
                LogWarning(string.Format("Instance {0} could not be terminated.", instanceId));
        }
    }
}
