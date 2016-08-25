/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Web.Mvc;
using YetaWF.Core.Localize;

namespace YetaWF.Core.Models.Attributes {

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class PackageDomainValidationAttribute : System.ComponentModel.DataAnnotations.RegularExpressionAttribute, IClientValidatable {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public PackageDomainValidationAttribute() : base(@"^\s*[a-zA-Z0-9]{1,50}\s*$") {
            ErrorMessage = __ResStr("valPkgDomain", "The domain specified is invalid - Please use only letters and numbers");
        }

        public IEnumerable<ModelClientValidationRule> GetClientValidationRules(ModelMetadata metadata, ControllerContext context) {
            ErrorMessage = __ResStr("valPkgDomain2", "The domain specified is invalid ({0}) - Please use only letters and numbers", AttributeHelper.GetPropertyCaption(metadata));
            return new[] { new ModelClientValidationRegexRule(ErrorMessage, this.Pattern) };
        }
    }
}
