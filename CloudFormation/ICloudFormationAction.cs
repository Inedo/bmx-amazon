using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudFormation
{
    public interface ICloudFormationAction
    {
        string AccessKeyCleartext { get; set; }

        string SecretKeyCleartext { get; set; }

        bool EncryptKeys { get; set; }

        string Region { get; set; }
    }
}
