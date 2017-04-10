/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using YetaWF.Core.Localize;
#if MVC6
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
#else
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Models.Attributes {

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class GuidValidationAttribute : DataTypeAttribute, YIClientValidatable {

        [CombinedResources]
        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public GuidValidationAttribute() : base(DataType.Text) {
            ErrorMessage = __ResStr("valGuid", "The guid is invalid - it should be in the format '00000000-0000-0000-0000-000000000000'");
        }

        private static Regex _regex = new Regex(@"^\s*([a-f0-9]{8}\-[a-f0-9]{4}\-[a-f0-9]{4}\-[a-f0-9]{4}\-[a-f0-9]{12})\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

        public override bool IsValid(object value) {
            string valueAsString = value as string;
            if (string.IsNullOrWhiteSpace(valueAsString)) return true;
            return _regex.Match(valueAsString).Length > 0;
        }
#if MVC6
        public void AddValidation(ClientModelValidationContext context) {
            ErrorMessage = __ResStr("valGuid2", "The guid ({0}) is invalid - it should be in the format '00000000-0000-0000-0000-000000000000'", AttributeHelper.GetPropertyCaption(context.ModelMetadata));
            AttributeHelper.MergeAttribute(context.Attributes, "data-val-guid", ErrorMessage);
            AttributeHelper.MergeAttribute(context.Attributes, "data-val", "true");
        }
#else
        public IEnumerable<ModelClientValidationRule> GetClientValidationRules(ModelMetadata metadata, ControllerContext context) {
            ErrorMessage = __ResStr("valGuid2", "The guid ({0}) is invalid - it should be in the format '00000000-0000-0000-0000-000000000000'", AttributeHelper.GetPropertyCaption(metadata));
            yield return new ModelClientValidationRule {
                ErrorMessage = ErrorMessage,
                ValidationType = "guid",
            };
        }
#endif
    }
}
