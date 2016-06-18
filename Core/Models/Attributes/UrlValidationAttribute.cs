/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using YetaWF.Core.Localize;
using YetaWF.Core.Support;
using YetaWF.Core.Views.Shared;

namespace YetaWF.Core.Models.Attributes {

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class UrlValidationAttribute : DataTypeAttribute /*, IClientValidatable*/ {

        [CombinedResources]
        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public enum SchemaEnum {
            Any = 0,
            HttpOnly = 1,
            HttpsOnly = 2,
        }
        public UrlValidationAttribute(SchemaEnum remoteSchema = SchemaEnum.Any, UrlHelperEx.UrlTypeEnum urlType = UrlHelperEx.UrlTypeEnum.Remote) : base(DataType.Url) {
            RemoteSchema = remoteSchema;
            UrlType = urlType;
        }
        public SchemaEnum RemoteSchema { get; private set; }
        public UrlHelperEx.UrlTypeEnum UrlType { get; private set; }

        private Regex _regexLocal = new Regex(@"^\s*\/.*\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
        private Regex _regexLocalNew = new Regex(@"^\s*\/[^\.\&\*\,\?]*\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);// this may need to be more restrictive
        private Regex _regexRemote = new Regex(@"^\s*(http[s]{0,1}:){0,1}\/\/.*\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private Regex _regexHttpsRemote = new Regex(@"^\s*https:\/\/.*\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private Regex _regexHttpRemote = new Regex(@"^\s*http:\/\/.*\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private void SetMessage(object obj) {
            string caption = AttributeHelper.GetPropertyCaption(obj);
            if ((UrlType & (UrlHelperEx.UrlTypeEnum.Remote | UrlHelperEx.UrlTypeEnum.Local)) == (UrlHelperEx.UrlTypeEnum.Remote | UrlHelperEx.UrlTypeEnum.Local)) {
                switch (RemoteSchema) {
                    default:
                    case SchemaEnum.Any:
                        ErrorMessage = __ResStr("valUrl1", "The Url for '{0}' is invalid - it should be in the format '/someLocalPage', '//somedomain.com/page', 'http://somedomain.com/page' or 'https://somedomain.com/page'", caption);
                        break;
                    case SchemaEnum.HttpOnly:
                        ErrorMessage = __ResStr("valUrl2", "The Url for '{0}' is invalid - it should be in the format '/someLocalPage' or 'http://somedomain.com/page' - https is not allowed", caption);
                        break;
                    case SchemaEnum.HttpsOnly:
                        ErrorMessage = __ResStr("valUrl3", "The Url for '{0}' is invalid - it should be in the format '/someLocalPage' or 'https://somedomain.com/page' - It must be secure - http is not allowed", caption);
                        break;
                }
            } else if ((UrlType & UrlHelperEx.UrlTypeEnum.Remote) != 0) {
                switch (RemoteSchema) {
                    default:
                    case SchemaEnum.Any:
                        ErrorMessage = __ResStr("valUrl4", "The Url for '{0}' is invalid - it should be in the format '//somedomain.com/page', 'http://somedomain.com/page' or 'https://somedomain.com/page'", caption);
                        break;
                    case SchemaEnum.HttpOnly:
                        ErrorMessage = __ResStr("valUrl5", "The Url for '{0}' is invalid - it should be in the format 'http://somedomain.com/page' - https is not allowed", caption);
                        break;
                    case SchemaEnum.HttpsOnly:
                        ErrorMessage = __ResStr("valUrl6", "The Url for '{0}' is invalid - it should be in the format 'https://somedomain.com/page' - It must be secure - http is not allowed", caption);
                        break;
                }
            } else if ((UrlType & UrlHelperEx.UrlTypeEnum.Local) != 0) {
                ErrorMessage = __ResStr("valUrl7", "The Url for '{0}' is invalid - it should be in the format '/someLocalPage' defining a local Url on the current site", caption);
            } else if (UrlType == UrlHelperEx.UrlTypeEnum.New) {
                ErrorMessage = __ResStr("valUrl8", "The Url for '{0}' is invalid - it should be in the format '/someLocalPage' defining a new local page - local pages must start with '/' and can't use certain special characters like . , * ? & etc.", caption);
            } else {
                throw new InternalError("Invalid UrlType combination {0}", UrlType);
            }
        }
        protected override ValidationResult IsValid(object objValue, ValidationContext context) {
            if (objValue == null) return ValidationResult.Success;
            string value = objValue as string;
            if (string.IsNullOrWhiteSpace(value)) return ValidationResult.Success;

            SetMessage(context);
            if ((UrlType & UrlHelperEx.UrlTypeEnum.Remote) != 0) {
                if ((UrlType & UrlHelperEx.UrlTypeEnum.Local) != 0) {
                    if (_regexLocal.Match(value).Length > 0)
                        return ValidationResult.Success;
                }
                switch (RemoteSchema) {
                    default:
                    case SchemaEnum.Any:
                        return (_regexRemote.Match(value).Length > 0) ? ValidationResult.Success : new ValidationResult(ErrorMessage);
                    case SchemaEnum.HttpOnly:
                        return (_regexHttpRemote.Match(value).Length > 0) ? ValidationResult.Success : new ValidationResult(ErrorMessage);
                    case SchemaEnum.HttpsOnly:
                        return (_regexHttpsRemote.Match(value).Length > 0) ? ValidationResult.Success : new ValidationResult(ErrorMessage);
                }
            } else if ((UrlType & UrlHelperEx.UrlTypeEnum.Local) != 0) {
                return (_regexLocal.Match(value).Length > 0) ? ValidationResult.Success : new ValidationResult(ErrorMessage);
            } else if (UrlType == UrlHelperEx.UrlTypeEnum.New) {
                return (_regexLocalNew.Match(value).Length > 0) ? ValidationResult.Success : new ValidationResult(ErrorMessage);
            } else {
                throw new InternalError("Invalid UrlType combination {0}", UrlType);
            }
        }
    }
}
