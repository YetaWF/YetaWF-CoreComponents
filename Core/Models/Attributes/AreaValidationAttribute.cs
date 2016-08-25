/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Web.Mvc;
using YetaWF.Core.Localize;

namespace YetaWF.Core.Models.Attributes {

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class AreaValidationAttribute : System.ComponentModel.DataAnnotations.RegularExpressionAttribute, IClientValidatable {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public AreaValidationAttribute() : base(@"^\s*[a-zA-Z0-9]{1,50}_[a-zA-Z0-9]{1,50}\s*$") {
            ErrorMessage = __ResStr("valArea", "The area specified is invalid - Please use only letters and numbers in the format domain_product");
        }

        public IEnumerable<ModelClientValidationRule> GetClientValidationRules(ModelMetadata metadata, ControllerContext context) {
            ErrorMessage = __ResStr("valArea2", "The area specified is invalid ({0}) - Please use only letters and numbers in the format domain_product", AttributeHelper.GetPropertyCaption(metadata));
            return new[] { new ModelClientValidationRegexRule(ErrorMessage, this.Pattern) };
        }
    }
}
