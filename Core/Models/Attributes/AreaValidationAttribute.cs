/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using YetaWF.Core.Localize;
#if MVC6
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
#else
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Models.Attributes {

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class AreaValidationAttribute : System.ComponentModel.DataAnnotations.RegularExpressionAttribute, YIClientValidatable {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public AreaValidationAttribute() : base(@"^\s*[a-zA-Z0-9]{1,50}_[a-zA-Z0-9]{1,50}\s*$") {
            ErrorMessage = __ResStr("valArea", "The area specified is invalid - Please use only letters and numbers in the format domain_product");
        }
#if MVC6
        public void AddValidation(ClientModelValidationContext context) {
            ErrorMessage = __ResStr("valArea2", "The area specified is invalid ({0}) - Please use only letters and numbers in the format domain_product", AttributeHelper.GetPropertyCaption(context.ModelMetadata));
            AttributeHelper.MergeAttribute(context.Attributes, "data-val-regex", ErrorMessage);
            AttributeHelper.MergeAttribute(context.Attributes, "data-val-regex-pattern", this.Pattern);
            AttributeHelper.MergeAttribute(context.Attributes, "data-val", "true");
        }
#else
        public IEnumerable<ModelClientValidationRule> GetClientValidationRules(ModelMetadata metadata, ControllerContext context) {
            ErrorMessage = __ResStr("valArea2", "The area specified is invalid ({0}) - Please use only letters and numbers in the format domain_product", AttributeHelper.GetPropertyCaption(metadata));
            return new[] { new ModelClientValidationRegexRule(ErrorMessage, this.Pattern) };
        }
#endif

    }
}
