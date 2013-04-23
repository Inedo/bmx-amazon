using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Security.Cryptography;

namespace System
{
    public static class SystemExtensions
    {
        public static IDictionary<string,string> ParseNameValue(this string Value)
        {
            IDictionary<string, string> retVal = new Dictionary<string, string>();
            var start = Value.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var s in start)
            {
                var result = s.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                if (result.Length > 1)
                    retVal.Add(result[0], result[1]);
            }
            return retVal;
        }

        public static string AllPublicProps(this object Value)
        {
            StringBuilder sb = new StringBuilder();
            var props = Value.GetType().GetProperties();
            foreach(var p in props)
            {
                sb.AppendFormat("Property: {0} / Value: {1}", p.Name, p.GetValue(Value,null));
            }
            return sb.ToString();
        }

        public static string GetDisplayName(this Type Value, string Name)
        {
            string retVal = null;
            var prop = (from p in Value.GetProperties() where p.Name == Name select p).FirstOrDefault();
            if (null != prop)
            {
                var val = ((DisplayNameAttribute)(from a in prop.GetCustomAttributes(typeof(DisplayNameAttribute), true) select a).FirstOrDefault());
                if (null != val)
                    retVal = val.DisplayName;
            }
            return retVal;
        }

        public static string Substitute(this string Value, IDictionary<string,string> Variables)
        {
            StringBuilder retVal = new StringBuilder(Value);
            foreach (var v in Variables)
            {
                retVal.Replace(String.Format("%{0}%", v.Key), v.Value);
            }
            return retVal.ToString();
        }

        public static string Encrypt(this string Value, int EntropyLength = 0)
        {
            var val = UnicodeEncoding.Unicode.GetBytes(Value);
            if ((0 == EntropyLength) || (EntropyLength < 1) || (EntropyLength > int.MaxValue) )
                return Convert.ToBase64String(ProtectedData.Protect(val, new byte[0], DataProtectionScope.LocalMachine));
            else
            {
                var rng = new RNGCryptoServiceProvider();
                byte[] entropy = new byte[EntropyLength];
                rng.GetBytes(entropy);
                return Convert.ToBase64String(entropy) + "^" + Convert.ToBase64String(ProtectedData.Protect(val, entropy, DataProtectionScope.LocalMachine));
            }
        }

        public static string Decrypt(this string Value)
        {
            if (!Value.Contains('^'))
                return UnicodeEncoding.Unicode.GetString(ProtectedData.Unprotect(Convert.FromBase64String(Value),new byte[0], DataProtectionScope.LocalMachine));
            else
            {
                string val = Value.Substring(Value.IndexOf('^')+1);
                string ent = Value.Substring(0,Value.IndexOf('^'));
                return UnicodeEncoding.Unicode.GetString(ProtectedData.Unprotect(Convert.FromBase64String(val), Convert.FromBase64String(ent), DataProtectionScope.LocalMachine));
            }
        }

    }
}


namespace Amazon.CloudFormation.Model
{
    public static class CloudFomationModelExtensions
    {
        public static string AsString(this List<Output> Value)
        {
            StringBuilder retVal = new StringBuilder();
            foreach (var o in Value)
            {
                retVal.AppendFormat("{{{0} / {1}}}\r\n", o.OutputKey, o.OutputValue);
            }
            return retVal.ToString();
        }
    }
}