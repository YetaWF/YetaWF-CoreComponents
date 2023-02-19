/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using YetaWF.Core.Components;
using YetaWF.Core.Localize;
using YetaWF.Core.Log;
using YetaWF.Core.Support;

namespace YetaWF.Core.Models.Attributes {

    // VALIDATION
    // VALIDATION
    // VALIDATION

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class RecaptchaV2Attribute : ValidationAttribute {

        protected YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        private static string __ResStr(string name, string defaultValue, params object?[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        private static readonly HttpClientHandler Handler = new HttpClientHandler {
            AllowAutoRedirect = true,
            UseCookies = false,
        };
        private static readonly HttpClient Client = new HttpClient(Handler, true) {
            Timeout = new TimeSpan(0, 0, 20),
        };

        public RecaptchaV2Attribute(string message) : base(message) {
            ErrorMessage = message;
        }
        private new string ErrorMessage { get; set; }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext) {
            if (!Manager.HaveCurrentRequest) throw new InternalError("No HTTP context or request available");
            HttpRequest request = Manager.CurrentRequest;

            RecaptchaV2Data? rc = value as RecaptchaV2Data;
            if (rc == null || !rc.VerifyPresence) return ValidationResult.Success;

            string? response = request.Form["g-recaptcha-response"];//$$$$ needs fixing
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
            public List<string>? ErrorCodes { get; set; }
        }
        private bool ValidateCaptcha(RecaptchaV2Config config, string response, string ipAddress) {

            if (string.IsNullOrWhiteSpace(config.PublicKey) || string.IsNullOrWhiteSpace(config.PrivateKey))
                throw new InternalError("The public and/or private keys have not been configured for RecaptchaV2 handling");

            string? resp = null;
            try {
                using (var request = new HttpRequestMessage()) {
                    resp = Client.GetStringAsync($"https://www.google.com/recaptcha/api/siteverify?secret={config.PrivateKey}&response={response}&remoteIp={ipAddress}").Result;
                    Logging.AddLog($"Validating CaptchaV2 - received \"{resp}\"");
                    if (string.IsNullOrWhiteSpace(resp)) return false;
                    RecaptchaV2Response recaptchaResp = Utility.JsonDeserialize<RecaptchaV2Response>(resp);
                    return recaptchaResp.Success;
                }
            } catch (Exception exc) {
                Console.WriteLine(exc.Message);
                throw;
            }
        }
    }
}
