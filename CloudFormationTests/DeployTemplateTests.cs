using System;
using System.Collections.Generic;
using System.Text;
using MbUnit.Framework;

using Inedo.BuildMasterExtensions.Amazon.CloudFormation;

namespace CloudFormationTests
{
    
    [TestFixture]
    public class DeployTemplateTests
    {
        private DeployTemplateAction action;

        [SetUp]
        public void Setup()
        {
            action = new DeployTemplateAction();
            action.AccessKey = System.IO.File.ReadAllText(@"c:\temp\accesskey.txt");
            action.SecretKey = System.IO.File.ReadAllText(@"C:\temp\secretkey.txt");
            action.InitClient();
        }

        [Test]
        [Category("CloudFormation")]
        public void TestClientCreationWithValidCredentials()
        {
            var test = new DeployTemplateAction();
            test.AccessKey = System.IO.File.ReadAllText(@"c:\temp\accesskey.txt");
            test.SecretKey = System.IO.File.ReadAllText(@"C:\temp\secretkey.txt");
            test.InitClient();
            Assert.IsNotNull(test.client);
        }

        [Test]
        [Category("CloudFormation")]
        public void TestClientCreationWithInvalidCredentials()
        {
            var test = new DeployTemplateAction();
            test.AccessKey = "";
            test.SecretKey = "";
            test.InitClient();
            Assert.IsNull(test.client);
        }

        [Test]
        [Row("Key1=Value1\r\nKey2=Value2", "[{\"__type\":\"Amazon.CloudFormation.Model.Parameter, AWSSDK\",\"ParameterKey\":\"Key1\",\"ParameterValue\":\"Value1\"},{\"ParameterKey\":\"Key2\",\"ParameterValue\":\"Value2\"}]")]
        [Row("\r\nKey1=Value1\r\nKey2=Value2\r\n\r\n", "[{\"__type\":\"Amazon.CloudFormation.Model.Parameter, AWSSDK\",\"ParameterKey\":\"Key1\",\"ParameterValue\":\"Value1\"},{\"ParameterKey\":\"Key2\",\"ParameterValue\":\"Value2\"}]")]
        [Row("\r\nKey1=Value1\r\nKey2=Value2\r\n\r\n=\r\n", "[{\"__type\":\"Amazon.CloudFormation.Model.Parameter, AWSSDK\",\"ParameterKey\":\"Key1\",\"ParameterValue\":\"Value1\"},{\"ParameterKey\":\"Key2\",\"ParameterValue\":\"Value2\"}]")]
        [Category("CloudFormation")]
        public void TestGetParameters(string Value, string Expected)
        {
            Assert.AreEqual(Expected, ServiceStack.Text.JsonSerializer.SerializeToString(action.GetParameters(Value)));
        }

        [Test]
        [Row("Key1=Value1\r\nKey2=Value2", "[{\"__type\":\"Amazon.CloudFormation.Model.Tag, AWSSDK\",\"Key\":\"Key1\",\"Value\":\"Value1\"},{\"Key\":\"Key2\",\"Value\":\"Value2\"}]")]
        [Row("\r\nKey1=Value1\r\nKey2=Value2\r\n\r\n", "[{\"__type\":\"Amazon.CloudFormation.Model.Tag, AWSSDK\",\"Key\":\"Key1\",\"Value\":\"Value1\"},{\"Key\":\"Key2\",\"Value\":\"Value2\"}]")]
        [Row("\r\nKey1=Value1\r\nKey2=Value2\r\n\r\n=\r\n", "[{\"__type\":\"Amazon.CloudFormation.Model.Tag, AWSSDK\",\"Key\":\"Key1\",\"Value\":\"Value1\"},{\"Key\":\"Key2\",\"Value\":\"Value2\"}]")]
        [Category("CloudFormation")]
        public void TestGetTags(string Value, string Expected)
        {
            Assert.AreEqual(Expected, ServiceStack.Text.JsonSerializer.SerializeToString(action.GetTags(Value)));
        }

        [Test]
        [Row("", "[]")]
        [Row("CAPABILITY_IAM", "[\"CAPABILITY_IAM\"]")]
        [Row("CAPABILITY_IAM\r\n", "[\"CAPABILITY_IAM\"]")]
        [Row("CAPABILITY_IAM\r\nCAPABILITY_FOO", "[\"CAPABILITY_IAM\",\"CAPABILITY_FOO\"]")]
        [Category("CloudFormation")]
        public void TestGetCapabilities(string Value, string Expected)
        {
            Assert.AreEqual(Expected, ServiceStack.Text.JsonSerializer.SerializeToString(action.GetCapabilities(Value)));
        }

        [Test]
        [Category("AWS")]
        [Disable("This will cost money")]
        public void TestCreateStack()
        {
            action.Parameters = "KeyPairName=CloudFormationTest\r\nInstanceType=t1.micro\r\nRoles=Web-Server\r\nFeatures=NET-Framework PowerShell-ISE";
            action.StackName = "DevStackTest";
            action.Tags = "Build=" + action.StackName;
            action.Capabilities = "CAPABILITY_IAM";
            action.templateData = Encoding.UTF8.GetString(Properties.Resources.Windows_Roles_And_Features); // System.IO.File.ReadAllText(@"C:\Users\jkuemerle\Downloads\Windows_Roles_And_Features.template");
            string result = action.CreateStack(action.Capabilities, action.Parameters, action.Tags, action.StackName, action.templateData, action.BucketName, CloudFormationAction.FailureAction.ROLLBACK.ToString());
            Assert.IsNotEmpty(result);
            var delReq = new Amazon.CloudFormation.Model.DeleteStackRequest {StackName = action.StackName };
            var delResp = action.client.DeleteStack(delReq);
        }

        [Test]
        [Category("CloudFormation")]
        public void TestUnencryptedAccessKey()
        {
            var test = new DeployTemplateAction();
            test.EncryptKeys = false;
            test.AccessKeyCleartext = "foo";
            Assert.AreEqual(test.AccessKeyCleartext, test.AccessKey);
        }

        [Test]
        [Category("CloudFormation")]
        public void TestEncryptedAccessKey()
        {
            var test = new DeployTemplateAction();
            test.EncryptKeys = true;
            test.AccessKeyCleartext = "foo";
            Assert.AreNotEqual(test.AccessKeyCleartext, test.AccessKey);
        }

        [Test]
        [Category("CloudFormation")]
        public void TestUnencryptedSecretKey()
        {
            var test = new DeployTemplateAction();
            test.EncryptKeys = false;
            test.SecretKeyCleartext = "foo";
            Assert.AreEqual(test.SecretKeyCleartext, test.SecretKey);
        }

        [Test]
        [Category("CloudFormation")]
        public void TestEncryptedSecretKey()
        {
            var test = new DeployTemplateAction();
            test.EncryptKeys = true;
            test.SecretKeyCleartext = "foo";
            Assert.AreNotEqual(test.SecretKeyCleartext, test.SecretKey);
        }

    }
}
