/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using YetaWF.Core.Localize;
using YetaWF.Core.Support;

namespace YetaWF.Core.Models.Attributes {

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class ListNoDuplicatesAttribute : ValidationAttribute, YIClientValidation {

        private static string __ResStr(string name, string defaultValue, params object?[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public ListNoDuplicatesAttribute() { }

        protected override ValidationResult? IsValid(object? value, ValidationContext context) {
            if (value is List<string>) {
                List<string> list = (List<string>)value;
                var query = list.GroupBy(x => x).Where(g => g.Count() > 1).Select(y => y.Key).ToList();
                if (query.Count > 0)
                    return new ValidationResult(__ResStr("dup", "Duplicate entry found - {0}", query.First()));
            } else if (value is List<int>) {
                List<int> list = (List<int>)value;
                var query = list.GroupBy(x => x).Where(g => g.Count() > 1).Select(y => y.Key).ToList();
                if (query.Count > 0)
                    return new ValidationResult(__ResStr("dup", "Duplicate entry found - {0}", query.First()));
            }
            return ValidationResult.Success;
        }
        public ValidationBase? AddValidation(object container, PropertyData propData, string caption, YTagBuilder tag) {
            return new ValidationBase {
                Method = nameof(ListNoDuplicatesAttribute),
                Message = __ResStr("dupClient2", "Duplicate entry found in the field labeled '{0}'", caption),
            };
        }
    }
}
