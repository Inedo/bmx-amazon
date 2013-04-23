using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MbUnit.Framework;

namespace CloudFormationTests
{
    [TestFixture]
    public class ExtensionsTests
    {
        [Test]
        [Row("Key1=Value1\r\nKey2=Value2", "{\"Key1\":\"Value1\",\"Key2\":\"Value2\"}")]
        [Row("\r\nKey1=Value1\r\nKey2=Value2\r\n\r\n", "{\"Key1\":\"Value1\",\"Key2\":\"Value2\"}")]
        [Row("\r\nKey1=Value1\r\nKey2=Value2\r\n\r\n=\r\n", "{\"Key1\":\"Value1\",\"Key2\":\"Value2\"}")]
        [Category("Extensions")]
        public void TestParseNameValue(string Value, string Expected)
        {
            var result = Value.ParseNameValue();
            var expected = ServiceStack.Text.JsonSerializer.DeserializeFromString<IDictionary<string, string>>(Expected);
            var ser = ServiceStack.Text.JsonSerializer.SerializeToString(result);
            Assert.AreEqual(expected, result);
        }

        [Test]
        [Row("web_template_before", "web_template_after", "{\"RELNO\":\"0\",\"BLDNO\":\"0\",\"FOOBAR\":\"0\"}")]
        [Category("Extensions")]
        public void TestSubstitute(string ValueName, string ExpectedName, string Vars)
        {
            string value = Properties.Resources.ResourceManager.GetString(ValueName);
            var variables = ServiceStack.Text.JsonSerializer.DeserializeFromString<Dictionary<string, string>>(Vars);
            Assert.AreEqual(Properties.Resources.ResourceManager.GetString(ExpectedName), value.Substitute(variables));
        }

        [Test]
        [Row("")]
        [Row("foo")]
        [Category("Extensions")]
        public void TestEncryptWithNoEntropy(string Value)
        {
            Assert.IsNotEmpty(Value.Encrypt());
        }

        [Test]
        [Row("", 100)]
        [Row("foo", 100)]
        [Row("foo", 0)]
        [Row("foo", -1)]
        [Category("Extensions")]
        public void TestEncryptWithEntropy(string Value, int Length)
        {
            Assert.IsNotEmpty(Value.Encrypt(Length));
        }

        [Test]
        [Row("", 100)]
        [Row("foo", 100)]
        [Row("foo", 0)]
        [Row("foo", -1)]
        [Category("Extensions")]
        public void TestDecryptWithNoEntropy(string Value, int Length)
        {
            Assert.AreEqual(Value, Value.Encrypt(Length).Decrypt());
        }
    
    
    }
}
