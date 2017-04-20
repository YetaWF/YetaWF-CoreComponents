/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

#if MVC6
using Microsoft.AspNetCore.Mvc;
#else
using System.Web.Mvc;
#endif
using YetaWF.Core.Addons;
using YetaWF.Core.Identity;
using YetaWF.Core.Localize;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.SendEmail;
using YetaWF.Core.Support;

namespace YetaWF.Core.Controllers.Shared
{
    public class SMTPEmailController : YetaWFController
    {
        [HttpPost]
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
            string msg = this.__ResStr("emailSent", "A test email has just been sent to {0}", YetaWFManager.JserEncode(Manager.UserEmail));
            ContentResult cr = Content(
                string.Format(Basics.AjaxJavascriptReturn + "Y_Message('{0}');", msg)
            );
            return cr;
        }
    }
}
