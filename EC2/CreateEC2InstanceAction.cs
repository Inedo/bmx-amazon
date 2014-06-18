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
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateEC2InstanceAction"/> class.
        /// </summary>
        public CreateEC2InstanceAction()
        {
        }

        /// <summary>
        /// Gets or sets the image ID used to create the instance.
        /// </summary>
        [Persistent]
        public string AmiID { get; set; }
        /// <summary>
        /// Gets or sets an optional IP address to associate with the instance.
        /// </summary>
        [Persistent]
        public string IPAddress { get; set; }

        /// <summary>
        /// This method is called to execute the Action.
        /// </summary>
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

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            if (!string.IsNullOrEmpty(this.IPAddress))
                return string.Format("Create an Amazon EC2 instance associated with the IP \"{0}\" using the AMI \"{1}\"", this.IPAddress, this.AmiID);
            else
                return string.Format("Create an Amazon EC2 instance using the AMI \"{0}\"", this.AmiID);
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
