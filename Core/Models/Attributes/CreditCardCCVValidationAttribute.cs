/* Copyright © 2021 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using YetaWF.Core.Localize;

namespace YetaWF.Core.Models.Attributes {

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class CreditCardCCVValidationAttribute : RegexValidationBaseAttribute {

        private static string __ResStr(string name, string defaultValue, params object?[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public CreditCardCCVValidationAttribute() : base(@"^\s*([0-9]{3,4})\s*$",
                __ResStr("valCCV", "The CVV (Card Verification Value) is invalid - The CVV is printed on the back of your credit card - Usually the last 4 digits of your card number, followed by the CVV (3-4 digits) are shown. If your credit card has such a security code, please enter it here (if you do not enter it, your order may be delayed or rejected) - If your card has no such code, simply leave this field empty"),
                __ResStr("valCCV2", "The CCV (Card Verification Value) is invalid (field '{0}') - The CVV is printed on the back of your credit card - Usually the last 4 digits of your card number, followed by the CVV (3-4 digits) are shown. If your credit card has such a security code, please enter it here (if you do not enter it, your order may be delayed or rejected) - If your card has no such code, simply leave this field empty"),
                __ResStr("valCCV3", "The CCV (Card Verification Value) '{0}' is invalid - The CVV is printed on the back of your credit card - Usually the last 4 digits of your card number, followed by the CVV (3-4 digits) are shown. If your credit card has such a security code, please enter it here (if you do not enter it, your order may be delayed or rejected) - If your card has no such code, simply leave this field empty")
            ) { }
    }
}
