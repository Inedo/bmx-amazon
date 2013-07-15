using System;
using System.Collections.Generic;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web;

namespace Inedo.BuildMasterExtensions.Amazon.EC2
{
    /// <summary>
    /// Represents an action that creates a new instance on the Amazon EC2 cloud.
    /// </summary>
    [ActionProperties(
        "Terminate Amazon EC2 Instance",
        "Terminates an Amazon EC2 instance at the specified IP Address.",
        "Amazon")]
    [CustomEditor(typeof(TerminateEC2InstanceActionEditor))]
    public sealed class TerminateEC2InstanceAction : RemoteActionBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TerminateEC2InstanceAction"/> class.
        /// </summary>
        public TerminateEC2InstanceAction()
        {
        }

        /// <summary>
        /// Gets or sets the ID of the EC2 instance to terminate or its associated IP address.
        /// </summary>
        [Persistent]
        public string InstanceIdOrIPAddress { get; set; }

        /// <summary>
        /// This method is called to execute the Action.
        /// </summary>
        protected override void Execute()
        {
            if (string.IsNullOrEmpty(this.InstanceIdOrIPAddress))
                throw new InvalidOperationException("Instance ID is not specified.");

            var cfg = (AmazonConfigurer)GetExtensionConfigurer();
            if (string.IsNullOrEmpty(cfg.AccessKeyId) || string.IsNullOrEmpty(cfg.SecretAccessKey))
                throw new InvalidOperationException("A valid Amazon access key ID and secret access key pair must be specified in the EC2 extension configuration.");

            ExecuteRemoteCommand("term");
        }

        /// <summary>
        /// When implemented in a derived class, processes an arbitrary command
        /// on the appropriate server.
        /// </summary>
        /// <param name="name">Name of command to process.</param>
        /// <param name="args">Optional command arguments.</param>
        /// <returns>Result of the command.</returns>
        protected override string ProcessRemoteCommand(string name, string[] args)
        {
            LogInformation("Contacting Amazon EC2 Service...");

            var instanceId = this.InstanceIdOrIPAddress;

            var cfg = (AmazonConfigurer)GetExtensionConfigurer();
            var ec2 = global::Amazon.AWSClientFactory.CreateAmazonEC2Client(cfg.AccessKeyId, cfg.SecretAccessKey);

            System.Net.IPAddress address;
            if (System.Net.IPAddress.TryParse(instanceId, out address))
            {
                LogInformation(string.Format("Looking up instance ID for {0}", instanceId));

                var addrResult = ec2.DescribeAddresses(new global::Amazon.EC2.Model.DescribeAddressesRequest()
                {
                    PublicIp = new List<string>()
                    {
                        instanceId
                    }
                });

                if (!addrResult.IsSetDescribeAddressesResult() || !addrResult.DescribeAddressesResult.IsSetAddress() || addrResult.DescribeAddressesResult.Address.Count == 0)
                    throw new InvalidOperationException(string.Format("IP address {0} is not associated with an instance.", instanceId));

                var addressInfo = addrResult.DescribeAddressesResult.Address[0];
                instanceId = addressInfo.InstanceId;

                LogInformation("Disassociating IP address");
                ec2.DisassociateAddress(new global::Amazon.EC2.Model.DisassociateAddressRequest() { PublicIp = this.InstanceIdOrIPAddress });
            }

            LogInformation(string.Format("Terminating instance {0}", instanceId));

            var response = ec2.TerminateInstances(new global::Amazon.EC2.Model.TerminateInstancesRequest()
            {
                InstanceId = new List<string>()
                {
                    instanceId
                }
            });

            if (response.IsSetTerminateInstancesResult() && response.TerminateInstancesResult.IsSetTerminatingInstance() && response.TerminateInstancesResult.TerminatingInstance.Count > 0)
                LogInformation(string.Format("Instance {0} has been terminated.", instanceId));
            else
                LogWarning(string.Format("Instance {0} could not be terminated.", instanceId));

            return string.Empty;
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            System.Net.IPAddress address;

            if (string.IsNullOrEmpty(this.InstanceIdOrIPAddress))
                return "Terminate an Amazon EC2 instance";
            else if (System.Net.IPAddress.TryParse(this.InstanceIdOrIPAddress, out address))
                return string.Format("Terminate an Amazon EC2 instance at {0}", this.InstanceIdOrIPAddress);
            else
                return string.Format("Terminate an Amazon EC2 instance with ID \"{0}\"", this.InstanceIdOrIPAddress);
        }
        /// <summary>
        /// Returns a value indicating whether the extension's configurer currently needs to be
        /// configured.
        /// </summary>
        /// <returns>
        /// True if configurer requires configuration; otherwise false.
        /// </returns>
        public override bool IsConfigurerSettingRequired()
        {
            return false;
            //var configurer = Util.Actions.GetConfigurer<CreateEC2InstanceAction>() as AmazonConfigurer;

            //if (configurer != null)
            //    return string.IsNullOrEmpty(configurer.AccessKeyId) || string.IsNullOrEmpty(configurer.SecretAccessKey);
            //else
            //    return true;
        }
    }
}
