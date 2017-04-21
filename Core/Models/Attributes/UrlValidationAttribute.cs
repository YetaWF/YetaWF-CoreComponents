/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using YetaWF.Core.Localize;
using YetaWF.Core.Support;
using YetaWF.Core.Views.Shared;

namespace YetaWF.Core.Models.Attributes {

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class UrlValidationAttribute : ValidationAttribute, YIClientValidatable {

        [CombinedResources]
        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public enum SchemaEnum {
            Any = 0,
            HttpOnly = 1,
            HttpsOnly = 2,
        }
        public UrlValidationAttribute(SchemaEnum remoteSchema = SchemaEnum.Any, UrlHelperEx.UrlTypeEnum urlType = UrlHelperEx.UrlTypeEnum.Remote)  {
            RemoteSchema = remoteSchema;
            UrlType = urlType;
            ErrorMessage = __ResStr("valUrl", "The Url is invalid");
            Pattern = GetPattern();
        }
        private Regex Pattern { get; set; }

        public SchemaEnum RemoteSchema { get; private set; }
        public UrlHelperEx.UrlTypeEnum UrlType { get; private set; }

        private Regex _regexLocal = new Regex(@"^\s*\/.*\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
        private Regex _regexLocalNew = new Regex(@"^\s*\/[^\.\&\*\,\?]*\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);// this may need to be more restrictive
        private Regex _regexRemote = new Regex(@"^\s*(http[s]{0,1}:){0,1}\/\/.*\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private Regex _regexHttpsRemote = new Regex(@"^\s*https:\/\/.*\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private Regex _regexHttpRemote = new Regex(@"^\s*http:\/\/.*\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private string GetMessage(ValidationContext context) {
            string caption = AttributeHelper.GetPropertyCaption(context);
            if ((UrlType & (UrlHelperEx.UrlTypeEnum.Remote | UrlHelperEx.UrlTypeEnum.Local)) == (UrlHelperEx.UrlTypeEnum.Remote | UrlHelperEx.UrlTypeEnum.Local)) {
                switch (RemoteSchema) {
                    default:
                    case SchemaEnum.Any:
                        return __ResStr("valUrl1", "The Url for '{0}' is invalid - it should be in the format '/someLocalPage', '//somedomain.com/page', 'http://somedomain.com/page' or 'https://somedomain.com/page'", caption);
                    case SchemaEnum.HttpOnly:
                        return __ResStr("valUrl2", "The Url for '{0}' is invalid - it should be in the format '/someLocalPage' or 'http://somedomain.com/page' - https is not allowed", caption);
                    case SchemaEnum.HttpsOnly:
                        return __ResStr("valUrl3", "The Url for '{0}' is invalid - it should be in the format '/someLocalPage' or 'https://somedomain.com/page' - It must be secure - http is not allowed", caption);
                }
            } else if ((UrlType & UrlHelperEx.UrlTypeEnum.Remote) != 0) {
                switch (RemoteSchema) {
                    default:
                    case SchemaEnum.Any:
                        return __ResStr("valUrl4", "The Url for '{0}' is invalid - it should be in the format '//somedomain.com/page', 'http://somedomain.com/page' or 'https://somedomain.com/page'", caption);
                    case SchemaEnum.HttpOnly:
                        return __ResStr("valUrl5", "The Url for '{0}' is invalid - it should be in the format 'http://somedomain.com/page' - https is not allowed", caption);
                    case SchemaEnum.HttpsOnly:
                        return __ResStr("valUrl6", "The Url for '{0}' is invalid - it should be in the format 'https://somedomain.com/page' - It must be secure - http is not allowed", caption);
                }
            } else if ((UrlType & UrlHelperEx.UrlTypeEnum.Local) != 0) {
                return __ResStr("valUrl7", "The Url for '{0}' is invalid - it should be in the format '/someLocalPage' defining a local Url on the current site", caption);
            } else if (UrlType == UrlHelperEx.UrlTypeEnum.New) {
                return __ResStr("valUrl8", "The Url for '{0}' is invalid - it should be in the format '/someLocalPage' defining a new local page - local pages must start with '/' and can't use certain special characters like . , * ? & etc.", caption);
            } else {
                throw new InternalError("Invalid UrlType combination {0}", UrlType);
            }
        }
        protected virtual Regex GetPattern() {
            if ((UrlType & UrlHelperEx.UrlTypeEnum.Remote) != 0) {
                if ((UrlType & UrlHelperEx.UrlTypeEnum.Local) != 0)
                    return _regexLocal;
                switch (RemoteSchema) {
                    default:
                    case SchemaEnum.Any:
                        return _regexRemote;
                    case SchemaEnum.HttpOnly:
                        return _regexHttpRemote;
                    case SchemaEnum.HttpsOnly:
                        return _regexHttpsRemote;
                }
            } else if ((UrlType & UrlHelperEx.UrlTypeEnum.Local) != 0) {
                return _regexLocal;
            } else if (UrlType == UrlHelperEx.UrlTypeEnum.New) {
                return _regexLocalNew;
            } else {
                throw new InternalError("Invalid UrlType combination {0}", UrlType);
            }
        }
        public override bool IsValid(object objValue) {
            if (objValue == null) return true;
            string value = objValue as string;
            if (string.IsNullOrWhiteSpace(value)) return true;

            return Pattern.Match(value).Length > 0;
        }
        protected override ValidationResult IsValid(object objValue, ValidationContext context) {
            if (IsValid(objValue)) return ValidationResult.Success;
            return new ValidationResult(GetMessage(context));
        }
#if MVC6
        public void AddValidation(ClientModelValidationContext context) {
            ErrorMessage = __ResStr("valUrl", "The Url is invalid");
            AttributeHelper.MergeAttribute(context.Attributes, "data-val-regex", ErrorMessage);
            AttributeHelper.MergeAttribute(context.Attributes, "data-val-regex-pattern", this.Pattern);
            AttributeHelper.MergeAttribute(context.Attributes, "data-val", "true");
        }
#else
        public IEnumerable<ModelClientValidationRule> GetClientValidationRules(ModelMetadata metadata, ControllerContext context) {
            var rule = new ModelClientValidationRule {
                ErrorMessage = __ResStr("dupClient", "Duplicate entry found"),
                ValidationType = "regex",
            };
            rule.ValidationParameters.Add("pattern", Pattern);
            yield return rule;
        }
#endif
    }
}
