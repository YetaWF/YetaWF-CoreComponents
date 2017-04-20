/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using YetaWF.Core.Localize;
#if MVC6
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
#else
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Models.Attributes {

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class ListNoDuplicatesAttribute : ValidationAttribute, YIClientValidatable {

        [CombinedResources]
        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public ListNoDuplicatesAttribute() { }

        protected override ValidationResult IsValid(object value, ValidationContext context) {
            if (value is List<string>) {
                List<string> list = (List<string>)value;
                var query = list.GroupBy(x => x).Where(g => g.Count() > 1).Select(y => y.Key).ToList();
                if (query.Count > 0)
                    return new ValidationResult(__ResStr("dup", "Duplicate entry found - {0}", query.First()));
            }
            return ValidationResult.Success;
        }
#if MVC6
        public void AddValidation(ClientModelValidationContext context) {
            ErrorMessage = __ResStr("dupClient",  "Duplicate entry found");
            AttributeHelper.MergeAttribute(context.Attributes, "data-val-listnoduplicates", ErrorMessage);
            AttributeHelper.MergeAttribute(context.Attributes, "data-val", "true");
        }
#else
        public IEnumerable<ModelClientValidationRule> GetClientValidationRules(ModelMetadata metadata, ControllerContext context) {
            var rule = new ModelClientValidationRule {
                ErrorMessage = __ResStr("dupClient", "Duplicate entry found"),
                ValidationType = "listnoduplicates"
            };
            yield return rule;
        }
#endif
    }
}
