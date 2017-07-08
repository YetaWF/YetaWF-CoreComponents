/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using YetaWF.Core.Addons;
using YetaWF.Core.Identity;
using YetaWF.Core.Localize;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.SendEmail;
using YetaWF.Core.Support;
#if MVC6
using Microsoft.AspNetCore.Mvc;
#else
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Controllers.Shared {

    /// <summary>
    /// SmtpServer template support.
    /// </summary>
    public class SMTPEmailController : YetaWFController {

        /// <summary>
        /// Sends a test email. Used in conjunction with client-side code and the SmtpServer template.
        /// </summary>
        /// <param name="server">The email server name.</param>
        /// <param name="port">The email server port number.</param>
        /// <param name="authentication">Type of email server authentication.</param>
        /// <param name="username">The user name used for authentication with the email server.</param>
        /// <param name="password">The password used for authentication with the email server.</param>
        /// <param name="ssl">true if SSL is required when communicating with the email server, false otherwise.</param>
        /// <returns>An action result.</returns>
        /// <remarks>The test email is sent to the email address of the currently logged on user requesting the test email.</remarks>
        [AllowPost]
        [ResourceAuthorize(CoreInfo.Resource_SMTPServer_SendTestEmail)]
        [ExcludeDemoMode]
        public ActionResult SendTestEmail(string server, int port, SMTPServer.AuthEnum authentication, string username, string password, bool ssl) {
            SendEmail.SendEmail sendEmail = new SendEmail.SendEmail();
            string subject = this.__ResStr("emailSubj", "Test Message");
            object parms = new {
                Message = this.__ResStr("emailMessage", "Test Message - Site Settings / Email")
            };
            sendEmail.PrepareEmailMessage(server, port, ssl, authentication, username, password, null, Manager.UserEmail, subject, sendEmail.GetEmailFile(AreaRegistration.CurrentPackage, "SMTPServer Test Message.txt"), parms);
            sendEmail.Send();
            string msg = this.__ResStr("emailSent", "A test email has just been sent to {0}", Manager.UserEmail);
            ContentResult cr = Content(
                string.Format(Basics.AjaxJavascriptReturn + "Y_Message('{0}');", YetaWFManager.JserEncode(msg))
            );
            return cr;
        }
    }
}
