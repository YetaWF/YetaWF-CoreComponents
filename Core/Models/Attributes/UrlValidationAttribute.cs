/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using YetaWF.Core.Localize;
using YetaWF.Core.Support;

namespace YetaWF.Core.Models.Attributes {

    [Flags]
    public enum UrlTypeEnum {
        Local = 1, // Local Url starting with /
        Remote = 2, // Remote Url http:// https:// or /
        New = 4, // Local by definition
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class UrlValidationAttribute : RegexValidationBaseAttribute {

        private static string __ResStr(string name, string defaultValue, params object?[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public UrlValidationAttribute(SchemaEnum remoteSchema = SchemaEnum.Any, UrlTypeEnum urlType = UrlTypeEnum.Remote) : base(@"", "", "", "") {
            RemoteSchema = remoteSchema;
            UrlType = urlType;
            Pattern = GetPattern();
            ErrorMessage = GetMessage();
            ErrorMessageWithDataFormat = GetMessageWithData();
            ErrorMessageWithFieldFormat = GetMessageWithField();
        }
        public SchemaEnum RemoteSchema { get; private set; }
        public UrlTypeEnum UrlType { get; private set; }

        public enum SchemaEnum {
            Any = 0,
            HttpOnly = 1,
            HttpsOnly = 2,
        }
        private string _regexLocalAndRemote = @"^(\s*((http[s]{0,1}:){0,1}\/\/.+|\/.*)\s*|)$";
        private string _regexLocal = @"^(\s*\/.*\s*|)$";
        private string _regexLocalNew = @"^(\s*\/[^\&\*\,\?]*\s*|)$";// this may need to be more restrictive
        private string _regexRemote = @"^(\s*(http[s]{0,1}\:){0,1}\/\/.+\s*|)$";
        private string _regexHttpsRemote = @"^(\s*https:\/\/.+\s*|)$";
        private string _regexHttpRemote = @"^(\s*http:\/\/.+\s*|)$";

        private string GetMessage() {
            if ((UrlType & (UrlTypeEnum.Remote | UrlTypeEnum.Local)) == (UrlTypeEnum.Remote | UrlTypeEnum.Local)) {
                switch (RemoteSchema) {
                    default:
                    case SchemaEnum.Any:
                        return __ResStr("valUrl1", "The Url is invalid - It should be in the format '/someLocalPage', '//somedomain.com/page', 'http://somedomain.com/page' or 'https://somedomain.com/page'");
                    case SchemaEnum.HttpOnly:
                        return __ResStr("valUrl2", "The Url is invalid - It should be in the format '/someLocalPage' or 'http://somedomain.com/page' - https is not allowed");
                    case SchemaEnum.HttpsOnly:
                        return __ResStr("valUrl3", "The Url is invalid - It should be in the format '/someLocalPage' or 'https://somedomain.com/page' - It must be secure - http is not allowed");
                }
            } else if ((UrlType & UrlTypeEnum.Remote) != 0) {
                switch (RemoteSchema) {
                    default:
                    case SchemaEnum.Any:
                        return __ResStr("valUrl4", "The Url is invalid - It should be in the format '//somedomain.com/page', 'http://somedomain.com/page' or 'https://somedomain.com/page'");
                    case SchemaEnum.HttpOnly:
                        return __ResStr("valUrl5", "The Url is invalid - It should be in the format 'http://somedomain.com/page' - https is not allowed");
                    case SchemaEnum.HttpsOnly:
                        return __ResStr("valUrl6", "The Url is invalid - It should be in the format 'https://somedomain.com/page' - It must be secure - http is not allowed");
                }
            } else if ((UrlType & UrlTypeEnum.Local) != 0) {
                return __ResStr("valUrl7", "The Url is invalid - It should be in the format '/someLocalPage' defining a local Url on the current site");
            } else if (UrlType == UrlTypeEnum.New) {
                return __ResStr("valUrl8", "The Url is invalid - It should be in the format '/someLocalPage' defining a new local page - local pages must start with '/' and can't use certain special characters like . , * ? & etc.");
            } else {
                throw new InternalError("Invalid UrlType combination {0}", UrlType);
            }
        }
        private string GetMessageWithData() {
            if ((UrlType & (UrlTypeEnum.Remote | UrlTypeEnum.Local)) == (UrlTypeEnum.Remote | UrlTypeEnum.Local)) {
                switch (RemoteSchema) {
                    default:
                    case SchemaEnum.Any:
                        return __ResStr("valUrlD1", "The Url '{0}' is invalid - It should be in the format '/someLocalPage', '//somedomain.com/page', 'http://somedomain.com/page' or 'https://somedomain.com/page'");
                    case SchemaEnum.HttpOnly:
                        return __ResStr("valUrlD2", "The Url '{0}' is invalid - It should be in the format '/someLocalPage' or 'http://somedomain.com/page' - https is not allowed");
                    case SchemaEnum.HttpsOnly:
                        return __ResStr("valUrlD3", "The Url '{0}' is invalid - It should be in the format '/someLocalPage' or 'https://somedomain.com/page' - It must be secure - http is not allowed");
                }
            } else if ((UrlType & UrlTypeEnum.Remote) != 0) {
                switch (RemoteSchema) {
                    default:
                    case SchemaEnum.Any:
                        return __ResStr("valUrlD4", "The Url '{0}' is invalid - It should be in the format '//somedomain.com/page', 'http://somedomain.com/page' or 'https://somedomain.com/page'");
                    case SchemaEnum.HttpOnly:
                        return __ResStr("valUrlD5", "The Url '{0}' is invalid - It should be in the format 'http://somedomain.com/page' - https is not allowed");
                    case SchemaEnum.HttpsOnly:
                        return __ResStr("valUrlD6", "The Url '{0}' is invalid - It should be in the format 'https://somedomain.com/page' - It must be secure - http is not allowed");
                }
            } else if ((UrlType & UrlTypeEnum.Local) != 0) {
                return __ResStr("valUrlD7", "The Url '{0}' is invalid - It should be in the format '/someLocalPage' defining a local Url on the current site");
            } else if (UrlType == UrlTypeEnum.New) {
                return __ResStr("valUrlD8", "The Url '{0}' is invalid - It should be in the format '/someLocalPage' defining a new local page - local pages must start with '/' and can't use certain special characters like . , * ? & etc.");
            } else {
                throw new InternalError("Invalid UrlType combination {0}", UrlType);
            }
        }
        private string GetMessageWithField() {
            if ((UrlType & (UrlTypeEnum.Remote | UrlTypeEnum.Local)) == (UrlTypeEnum.Remote | UrlTypeEnum.Local)) {
                switch (RemoteSchema) {
                    default:
                    case SchemaEnum.Any:
                        return __ResStr("valUrlF1", "The Url is invalid (field '{0}') - It should be in the format '/someLocalPage', '//somedomain.com/page', 'http://somedomain.com/page' or 'https://somedomain.com/page'");
                    case SchemaEnum.HttpOnly:
                        return __ResStr("valUrlF2", "The Url is invalid (field '{0}') - It should be in the format '/someLocalPage' or 'http://somedomain.com/page' - https is not allowed");
                    case SchemaEnum.HttpsOnly:
                        return __ResStr("valUrlF3", "The Url is invalid (field '{0}') - It should be in the format '/someLocalPage' or 'https://somedomain.com/page' - It must be secure - http is not allowed");
                }
            } else if ((UrlType & UrlTypeEnum.Remote) != 0) {
                switch (RemoteSchema) {
                    default:
                    case SchemaEnum.Any:
                        return __ResStr("valUrlF4", "The Url is invalid (field '{0}') - It should be in the format '//somedomain.com/page', 'http://somedomain.com/page' or 'https://somedomain.com/page'");
                    case SchemaEnum.HttpOnly:
                        return __ResStr("valUrlF5", "The Url is invalid (field '{0}') - It should be in the format 'http://somedomain.com/page' - https is not allowed");
                    case SchemaEnum.HttpsOnly:
                        return __ResStr("valUrlF6", "The Url is invalid (field '{0}') - It should be in the format 'https://somedomain.com/page' - It must be secure - http is not allowed");
                }
            } else if ((UrlType & UrlTypeEnum.Local) != 0) {
                return __ResStr("valUrlF7", "The Url is invalid (field '{0}') - It should be in the format '/someLocalPage' defining a local Url on the current site");
            } else if (UrlType == UrlTypeEnum.New) {
                return __ResStr("valUrlF8", "The Url is invalid (field '{0}') - It should be in the format '/someLocalPage' defining a new local page - local pages must start with '/' and can't use certain special characters like . , * ? & etc.");
            } else {
                throw new InternalError("Invalid UrlType combination {0}", UrlType);
            }
        }
        protected string GetPattern() {
            if ((UrlType & UrlTypeEnum.Remote) != 0) {
                if ((UrlType & UrlTypeEnum.Local) != 0)
                    return _regexLocalAndRemote;
                switch (RemoteSchema) {
                    default:
                    case SchemaEnum.Any:
                        return _regexRemote;
                    case SchemaEnum.HttpOnly:
                        return _regexHttpRemote;
                    case SchemaEnum.HttpsOnly:
                        return _regexHttpsRemote;
                }
            } else if ((UrlType & UrlTypeEnum.Local) != 0) {
                return _regexLocal;
            } else if (UrlType == UrlTypeEnum.New) {
                return _regexLocalNew;
            } else {
                throw new InternalError("Invalid UrlType combination {0}", UrlType);
            }
        }
    }
}
