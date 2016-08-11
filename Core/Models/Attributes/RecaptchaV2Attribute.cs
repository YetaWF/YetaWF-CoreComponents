/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using YetaWF.Core.Addons;
using YetaWF.Core.Localize;
using YetaWF.Core.Log;
using YetaWF.Core.Support;
using YetaWF.Core.Views.Shared;

namespace YetaWF.Core.Models.Attributes {

    // VALIDATION
    // VALIDATION
    // VALIDATION

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class RecaptchaV2Attribute : ValidationAttribute {

        protected YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        [CombinedResources]
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

            RecaptchaV2Config config = RecaptchaV2Config.LoadRecaptchaV2Config();
            if (string.IsNullOrWhiteSpace(config.PublicKey) || string.IsNullOrWhiteSpace(config.PrivateKey))
                throw new Error(__ResStr("errPrivateKey", "The RecaptchaV2 configuration settings are missing - no public/private key found"));

            if (ValidateCaptcha(response, request.UserHostAddress))
                return ValidationResult.Success;

            return new ValidationResult(ErrorMessage);
        }

        public class RecaptchaV2Response {
            public bool Success { get; set; }
            public List<string> ErrorCodes { get; set; }
        }
        private bool ValidateCaptcha(string response, string ipAddress) {
            using (WebClient client = new WebClient()) {
                RecaptchaV2Config config = RecaptchaV2Config.LoadRecaptchaV2Config();
                if (string.IsNullOrWhiteSpace(config.PublicKey) || string.IsNullOrWhiteSpace(config.PrivateKey))
                    throw new InternalError("The public and/or private keys have not been configured for RecaptchaV2 handling");
                string resp = client.DownloadString(string.Format("https://www.google.com/recaptcha/api/siteverify?secret={0}&response={1}&remoteIp={2}",
                    config.PrivateKey, response, ipAddress));
                Logging.AddLog("Validating CaptchaV2 - received \"{0}\"", resp);
                JavaScriptSerializer jser = YetaWFManager.Jser;
                RecaptchaV2Response recaptchaResp = jser.Deserialize<RecaptchaV2Response>(resp);
                return recaptchaResp.Success;
            }
        }
    }
}
