/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using YetaWF.Core.Localize;

namespace YetaWF.Core.Models.Attributes {

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class CreditCardCCVValidationAttribute : DataTypeAttribute {

        [CombinedResources]
        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public CreditCardCCVValidationAttribute() : base(DataType.Text) {
            ErrorMessage = __ResStr("valCCV", "The card ID is invalid - The Card ID is printed on the back of your credit card. Usually the last 4 digits of your card number, followed by the card ID (3-4 digits) are shown. If your credit card has such a security code, please enter it here (if you do not enter it, your order may be delayed or rejected). If your card has no such code, simply leave this field empty.");
        }

        private static Regex _regex = new Regex(@"^\s*([0-9]{3,4})\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

        public override bool IsValid(object value) {
            string valueAsString = value as string;
            if (string.IsNullOrWhiteSpace(valueAsString)) return true;
            return _regex.Match(valueAsString).Length > 0;
        }
    }
}
