/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using YetaWF.Core;
using YetaWF.Core.Addons;
using YetaWF.Core.Endpoints;
using YetaWF.Core.Endpoints.Filters;
using YetaWF.Core.Localize;
using YetaWF.Core.Packages;
using YetaWF.Core.SendEmail;
using YetaWF.Core.Support;

namespace YetaWF.Modules.ComponentsHTML.Endpoints;

/// <summary>
/// Endpoints for the SmtpServer template.
/// </summary>
public class SMTPServerEndpoints : YetaWFEndpoints {

    internal const string SendTestEmail = "SendTestEmail";

    private static string __ResStr(string name, string defaultValue, params object?[] parms) { return ResourceAccess.GetResourceString(typeof(SMTPServerEndpoints), name, defaultValue, parms); }

    /// <summary>
    /// Registers endpoints for the SmtpServer template.
    /// </summary>
    public static void RegisterEndpoints(IEndpointRouteBuilder endpoints, Package package, string areaName) {

        RouteGroupBuilder group = endpoints.MapGroup(GetPackageApiRoute(package, typeof(SMTPServerEndpoints)))
            .RequireAuthorization()
            .AntiForgeryToken();

        // Saves an uploaded image file. Works in conjunction with the SmtpServer template and YetaWF.Core.Upload.FileUpload.
        group.MapPost(SendTestEmail, async (HttpContext context, Guid __ModuleGuid, string server, int port, SMTPServer.AuthEnum authentication, string username, string password, bool ssl) => {
            SendEmail sendEmail = new SendEmail();
            string subject = __ResStr("emailSubj", "Test Message");
            object parms = new {
                Message = __ResStr("emailMessage", "Test Message - Site Settings / Email")
            };
            await sendEmail.PrepareEmailMessageAsync(server, port, ssl, authentication, username, password, null, Manager.UserEmail, subject, await sendEmail.GetEmailFileAsync(AreaRegistration.CurrentPackage, "SMTPServer Test Message.txt"), parms);
            await sendEmail.SendAsync();
            string msg = __ResStr("emailSent", "A test email has just been sent to {0}", Manager.UserEmail);
            return Results.Text($"{Basics.AjaxJavascriptReturn}$YetaWF.message('{Utility.JserEncode(msg)}');", "application/json");
        })
            .ResourceAuthorize(CoreInfo.Resource_UploadImages);
    }
}
