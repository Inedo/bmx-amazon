using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;
using System.Linq.Expressions;

namespace CloudFormation
{
    public abstract class CloudFormationActionEditor : ActionEditorBase
    {
        protected ValidatingTextBox txtAccessKey;
        protected ValidatingTextBox txtSecretKey;
        protected CheckBox chkEncryptKeys;
        protected DropDownList ddlRegion;

        public override void BindToForm(ActionBase extension)
        {
            this.EnsureChildControls();
            var action = extension as ICloudFormationAction ;
            if (null != action)
            {
                txtAccessKey.Text = action.AccessKeyCleartext;
                txtSecretKey.Text = action.SecretKeyCleartext;
                chkEncryptKeys.Checked = action.EncryptKeys;
                ddlRegion.SelectedValue = action.Region;
            }
        }

        protected override void CreateChildControls()
        {
            txtAccessKey = new ValidatingTextBox { Width = 400 };
            txtSecretKey = new ValidatingTextBox { Width = 400 };
            chkEncryptKeys = new CheckBox { Width = 400 };
            ddlRegion = new DropDownList { Width = 400 };
            ddlRegion.Items.AddRange((from r in Amazon.RegionEndpoint.EnumerableAllRegions select new ListItem { Text = r.DisplayName, Value = r.SystemName }).ToArray());
            this.Controls.Add(
                new FormFieldGroup("Credentials", "AWS Service Credentials", false,
                    new StandardFormField("Access Key", txtAccessKey),
                    new StandardFormField("Secret Key", txtSecretKey),
                    new StandardFormField("Encrypt Keys in database", chkEncryptKeys),
                    new StandardFormField("Region",ddlRegion)
                )
            );
        }

    }
}
