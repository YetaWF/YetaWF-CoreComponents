/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using YetaWF.Core.Localize;
#if MVC6
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
#else
using System.Collections.Generic;
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Models.Attributes {

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class PackageDomainValidationAttribute : System.ComponentModel.DataAnnotations.RegularExpressionAttribute, YIClientValidatable {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public PackageDomainValidationAttribute() : base(@"^\s*[a-zA-Z0-9]{1,50}\s*$") {
            ErrorMessage = __ResStr("valPkgDomain", "The domain specified is invalid - Please use only letters and numbers");
        }
#if MVC6
        public void AddValidation(ClientModelValidationContext context) {
            ErrorMessage = __ResStr("valPkgDomain2", "The domain specified is invalid ({0}) - Please use only letters and numbers", AttributeHelper.GetPropertyCaption(context.ModelMetadata));
            AttributeHelper.MergeAttribute(context.Attributes, "data-val-regex", ErrorMessage);
            AttributeHelper.MergeAttribute(context.Attributes, "data-val-regex-pattern", this.Pattern);
            AttributeHelper.MergeAttribute(context.Attributes, "data-val", "true");
        }
#else
        public IEnumerable<ModelClientValidationRule> GetClientValidationRules(ModelMetadata metadata, ControllerContext context) {
            ErrorMessage = __ResStr("valPkgDomain2", "The domain specified is invalid ({0}) - Please use only letters and numbers", AttributeHelper.GetPropertyCaption(metadata));
            return new[] { new ModelClientValidationRegexRule(ErrorMessage, this.Pattern) };
        }
#endif

    }
}
