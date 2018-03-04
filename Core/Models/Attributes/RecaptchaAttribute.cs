/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text;
#if MVC6
using Microsoft.AspNetCore.Http;
#else
using System.Web;
#endif
using YetaWF.Core.Localize;
using YetaWF.Core.Log;
using YetaWF.Core.Support;
using YetaWF.Core.Views.Shared;

namespace YetaWF.Core.Models.Attributes {

    // VALIDATION
    // VALIDATION
    // VALIDATION

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class RecaptchaAttribute : ValidationAttribute {

        protected YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        [CombinedResources]
        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public RecaptchaAttribute(string message)
            : base(message) {
            ErrorMessage = message;
        }
        private new string ErrorMessage { get; set; }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext) {
            if (!Manager.HaveCurrentRequest) throw new InternalError("No http context or request available");
            HttpRequest request = Manager.CurrentRequest;

            RecaptchaData rc = value as RecaptchaData;
            if (rc == null || !rc.VerifyPresence) return ValidationResult.Success;

            string challenge;
            string response;
#if MVC6
            challenge = request.Query["recaptcha_challenge_field"];
            response = request.Query["recaptcha_response_field"];
#else
            challenge = request["recaptcha_challenge_field"];
            response = request["recaptcha_response_field"];
#endif
            if (string.IsNullOrWhiteSpace(challenge) || string.IsNullOrWhiteSpace(response))
                return new ValidationResult(ErrorMessage);

            RecaptchaConfig config = RecaptchaConfig.LoadRecaptchaConfig().Result;
            if (string.IsNullOrWhiteSpace(config.PrivateKey))
                throw new Error(__ResStr("errPrivateKeyV2", "The Recaptcha configuration settings are missing - no private key found"));
            using (WebClient client = new WebClient()) {
                byte[] responseBytes = client.UploadValues("http://www.google.com/recaptcha/api/verify", new NameValueCollection() {
                    { "privatekey", config.PrivateKey },
                    { "remoteip", Manager.UserHostAddress },
                    { "challenge", challenge },
                    { "response", response },
                });
                string resp = Encoding.UTF8.GetString(responseBytes, 0, responseBytes.Length);
                Logging.AddLog("Validating Captcha - received \"{0}\"", resp);
                string[] lines = resp.Split(new char[] { '\r', '\n' });
                if (lines[0] == "true")
                    return ValidationResult.Success;
            }
            return new ValidationResult(ErrorMessage);
        }
    }
}
