/* Copyright © 2021 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using YetaWF.Core.Localize;
using YetaWF.Core.Support;

namespace YetaWF.Core.Models.Attributes {

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class EmailValidationAttribute : DataTypeAttribute /*, YIClientValidation - disable as too confusing */ {

        private static string __ResStr(string name, string defaultValue, params object?[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public EmailValidationAttribute() : base(DataType.EmailAddress) {
            ErrorMessage = __ResStr("valEmail", "The email address is invalid - it should be in the format 'user@domain' where domain can be any domain like domain.com, domain.net, domain.io, etc.");
        }

        // aligned with jquery.validate.js
        private static Regex _regex = new Regex(@"^\s*[a-zA-Z0-9\.!#$%&'*+\/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

        public override bool IsValid(object? value) {
            if (value == null) return true;
            if (value is string) {
                string valueAsString = (string)value;
                if (string.IsNullOrWhiteSpace(valueAsString)) return true;
                if (_regex.Match(valueAsString).Length == 0) {
                    ErrorMessage = __ResStr("valEmail3", "The email address {0} is invalid - it should be in the format 'user@domain' where domain can be any domain like domain.com, domain.net, domain.io, etc.", valueAsString);
                    return false;
                }
                return true;
            } else if (value is List<string>) {
                List<string> list = (List<string>)value;
                foreach (string l in list) {
                    if (!IsValid(l)) return false;
                }
                return true;
            } else
                throw new InternalError("Invalid type used for EmailValidationAttribute - {0}", value.GetType().FullName);
        }
        //public void AddValidation(object container, PropertyData propData, YTagBuilder tag) {
        //    string msg = __ResStr("valEmail2", "The email address for the field labeled '{0}' is invalid - it should be in the format 'user@domain' where domain can be any domain like domain.com, domain.net, domain.io, etc.", propData.GetCaption(container));
        //    tag.MergeAttribute("data-val-email", msg);
        //    tag.MergeAttribute("data-val", "true");
        //}
    }
}
