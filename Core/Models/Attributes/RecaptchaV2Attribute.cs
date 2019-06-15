/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using YetaWF.Core.Addons;
using YetaWF.Core.Localize;
using YetaWF.Core.Log;
using YetaWF.Core.Support;
using YetaWF.Core.Components;
#if MVC6
using Microsoft.AspNetCore.Http;
#else
using System.Web;
#endif

namespace YetaWF.Core.Models.Attributes {

    // VALIDATION
    // VALIDATION
    // VALIDATION

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class RecaptchaV2Attribute : ValidationAttribute {

        protected YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public RecaptchaV2Attribute(string message) : base(message) {
            ErrorMessage = message;
        }
        private new string ErrorMessage { get; set; }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext) {
            if (!Manager.HaveCurrentRequest) throw new InternalError("No http context or request available");
            HttpRequest request = Manager.CurrentRequest;

            RecaptchaV2Data rc = value as RecaptchaV2Data;
            if (rc == null || !rc.VerifyPresence) return ValidationResult.Success;

            string response = request.Form[Basics.RecaptchaV2Parm];
            if (string.IsNullOrWhiteSpace(response))
                return new ValidationResult(ErrorMessage);

            RecaptchaV2Config config = YetaWFManager.Syncify(async () =>
                await RecaptchaV2Config.LoadRecaptchaV2ConfigAsync()
            );
            if (string.IsNullOrWhiteSpace(config.PublicKey) || string.IsNullOrWhiteSpace(config.PrivateKey))
                throw new Error(__ResStr("errPrivateKey", "The RecaptchaV2 configuration settings are missing - no public/private key found"));

            if (ValidateCaptcha(config, response, Manager.UserHostAddress))
                return ValidationResult.Success;

            return new ValidationResult(ErrorMessage);
        }

        public class RecaptchaV2Response {
            public bool Success { get; set; }
            public List<string> ErrorCodes { get; set; }
        }
        private bool ValidateCaptcha(RecaptchaV2Config config, string response, string ipAddress) {
            using (WebClient client = new WebClient()) {
                if (string.IsNullOrWhiteSpace(config.PublicKey) || string.IsNullOrWhiteSpace(config.PrivateKey))
                    throw new InternalError("The public and/or private keys have not been configured for RecaptchaV2 handling");
                string resp = client.DownloadString(string.Format("https://www.google.com/recaptcha/api/siteverify?secret={0}&response={1}&remoteIp={2}",
                    config.PrivateKey, response, ipAddress));
                Logging.AddLog("Validating CaptchaV2 - received \"{0}\"", resp);
                RecaptchaV2Response recaptchaResp = Utility.JsonDeserialize<RecaptchaV2Response>(resp);
                return recaptchaResp.Success;
            }
        }
    }
}
