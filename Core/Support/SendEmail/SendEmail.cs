﻿/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Net.Mime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YetaWF.Core.Addons;
using YetaWF.Core.IO;
using YetaWF.Core.Localize;
using YetaWF.Core.Log;
using YetaWF.Core.Packages;
using YetaWF.Core.Support;

namespace YetaWF.Core.SendEmail {

    /// <summary>
    /// Any class implementing this interface acts as a SendEmail provider, replacing the built-in email sending support.
    /// </summary>
    /// <remarks>There can only be on SendEmail provider.</remarks>
    public interface ISendEmail {
        Task<object> PrepareEmailMessageAsync(string? toEmail, string subject, string emailFile, string? fromEmail = null, object? Parameters = null, NameValueCollection? Headers = null);
        Task<string> GetEmailFileAsync(Package package, string filename);
        void AddBcc(object sendEmailData, string ccEmail);
        Task SendAsync(object sendEmailData, bool fThrowError = true);
        /// <summary>
        /// Returns the sending email address. Only valid after the email has been sent.
        /// </summary>
        /// <returns>The sending email address.</returns>
        Task<string> GetSendingEmailAddressAsync(object sendEmailData);
    }

    public class SendEmail : IInitializeApplicationStartup {

        public static ISendEmail? SendEmailProvider = null;
        public object SendEmailData = null!;

        private string SendingEmailAddress = null!;

        public Task InitializeApplicationStartupAsync() {
            List<Type> types = Package.GetClassesInPackages<ISendEmail>();
            if (types.Count > 0) {
                // Installing sendemail provider
                if (types.Count > 1)
                    throw new InternalError("More than 1 SendEmail provider installed");
                Type type = types[0];
                ISendEmail? isendEmail = (ISendEmail?)Activator.CreateInstance(type);
                if (isendEmail != null)
                    SendEmailProvider = isendEmail;
                else
                    throw new InternalError($"Unable to create ISendEmail provider {type.FullName}");
            }
            return Task.CompletedTask;
        }
        void NoSendEmailProviderAllowed() {
            if (SendEmailProvider != null)
                throw new InternalError("Not supported when a SendEmail provider is installed");
        }

        public const string EmailsFolder = "Emails";
        public const string EmailHtmlExtension = ".html";
        public const string EmailTxtExtension = ".txt";

        internal static HttpClient Client = new HttpClient();

        public SendEmail() { }

        protected YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        public MailMessage MailMessage { get; protected set; } = null!;
        protected SmtpClient SmtpClient { get; set; } = null!;

        public async Task<string> GetSendingEmailAddressAsync() {
            string email;
            if (SendEmailProvider != null) {
                email = await SendEmailProvider.GetSendingEmailAddressAsync(SendEmailData);
            } else {
                email = SendingEmailAddress;
            }
            if (string.IsNullOrWhiteSpace(email))
                email = this.__ResStr("n/a", "(n/a)");
            return email;
        }

        public async Task PrepareEmailMessageFromStringsAsync(string toEmail, string subject, string emailText, string emailHTML, string? fromEmail = null, object? Parameters = null, NameValueCollection? Headers = null) {
            NoSendEmailProviderAllowed();
            Manager.CurrentSite.SMTP.Validate();
            SMTPServer smtpEmail = Manager.CurrentSite.SMTP;
            emailHTML = "<!DOCTYPE html><html><head>" +
                "<title>" + subject + "</title>" +
                "</head><body style='margin:0'>" + emailHTML + "</body></html>";
            await PrepareEmailMessageAsync(smtpEmail.Server!, smtpEmail.Port, smtpEmail.SSL, smtpEmail.Authentication, smtpEmail.UserName, smtpEmail.Password, null, toEmail, subject, emailText, emailHTML, null, Parameters, Headers);
        }
        public async Task<string> GetEmailFileAsync(Package package, string filename) {
            if (SendEmailProvider != null) {
                return await SendEmailProvider.GetEmailFileAsync(package, filename);
            } else {
                string moduleAddOnUrl = Package.GetAddOnPackageUrl(package.AreaName);
                string customModuleAddOnUrl = Package.GetCustomUrlFromUrl(moduleAddOnUrl);

                // locate site specific custom email
                string file = Utility.UrlToPhysical(string.Format("{0}/{1}/{2}", customModuleAddOnUrl, EmailsFolder, filename));
                if (await FileSystem.FileSystemProvider.FileExistsAsync(file))
                    return file;
                // otherwise use default email
                file = Utility.UrlToPhysical(string.Format("{0}/{1}/{2}", moduleAddOnUrl, EmailsFolder, filename));
                if (await FileSystem.FileSystemProvider.FileExistsAsync(file))
                    return file;
                throw new InternalError("Email configuration file {0} not found at {1}", filename, moduleAddOnUrl);
            }
        }
        public async Task PrepareEmailMessageAsync(string? toEmail, string subject, string emailFile, string? fromEmail = null, object? Parameters = null, NameValueCollection? Headers = null) {
            if (SendEmailProvider != null) {
                SendEmailData = await SendEmailProvider.PrepareEmailMessageAsync(toEmail, subject, emailFile, fromEmail, Parameters, Headers);
            } else {
                Manager.CurrentSite.SMTP.Validate();
                SMTPServer smtpEmail = Manager.CurrentSite.SMTP;
                await PrepareEmailMessageAsync(smtpEmail.Server!, smtpEmail.Port, smtpEmail.SSL, smtpEmail.Authentication, smtpEmail.UserName, smtpEmail.Password, fromEmail, toEmail, subject, emailFile, Parameters, Headers);
            }
        }
        public async Task PrepareEmailMessageAsync(string server, int port, bool ssl, SMTPServer.AuthEnum auth, string? username, string? password, string? fromEmail, string? toEmail, string subject, string emailFile, object? Parameters = null, NameValueCollection? Headers = null) {
            NoSendEmailProviderAllowed();
            string file = emailFile;
            if (!file.EndsWith(EmailTxtExtension, StringComparison.CurrentCultureIgnoreCase))
                throw new Error(this.__ResStr("errEmailTextInv", "The base email file {0} must be a text file (ending in .txt)"), file);

            Logging.AddLog("Sending email {0} to {1}", emailFile, toEmail);

            // read simple txt
            string linesText;
            try {
                linesText = await FileSystem.FileSystemProvider.ReadAllTextAsync(file);
            } catch (Exception exc) {
                throw new Error(this.__ResStr("errEmailTextFile", "The email {0} could not be found."), file, exc);
            }

            // add html formatted file if available
            file = Path.ChangeExtension(emailFile, EmailHtmlExtension);
            string? linesHtml = null, htmlFolder = null;
            try {
                linesHtml = await FileSystem.FileSystemProvider.ReadAllTextAsync(file);
                htmlFolder = Path.GetDirectoryName(file)!;
            } catch (Exception exc) {
                if (!(exc is FileNotFoundException))
                    Logging.AddErrorLog(this.__ResStr("errEmailHtmlFile", "The html formatted email {0} could not be read."), file, exc);
                linesHtml = null;
                htmlFolder = null;
            }
            await PrepareEmailMessageAsync(server, port, ssl, auth, username, password, fromEmail, toEmail, subject, linesText, linesHtml, htmlFolder, Parameters, Headers);
        }
        public async Task PrepareEmailMessageAsync(string server, int port, bool ssl, SMTPServer.AuthEnum auth, string? username, string? password, string? fromEmail, string? toEmail, string subject, string linesText, string? linesHtml, string? htmlFolder, object? Parameters = null, NameValueCollection? Headers = null) {
            NoSendEmailProviderAllowed();
            try {
                if (string.IsNullOrEmpty(server))
                    throw new Error(this.__ResStr("errMailServer", "No mail server specified."));

#if DEBUG
                bool debug = true;
#else
                bool debug = false;
#endif

                if (string.IsNullOrEmpty(toEmail) || Manager.CurrentSite.EmailDebug || debug) // force email address
                    toEmail = Manager.CurrentSite.AdminEmail;
                if (string.IsNullOrEmpty(toEmail))
                    throw new Error(this.__ResStr("errNoRecv", "No receiving email address specified - The site administrator's email address is used as receiving email address - The site administrator's email address can be defined using the Site Properties"));
                if (string.IsNullOrEmpty(fromEmail))
                    fromEmail = Manager.CurrentSite.AdminEmail;
                if (string.IsNullOrEmpty(fromEmail))
                    throw new Error(this.__ResStr("errNoSender", "No sending email address specified"));

                SendingEmailAddress = fromEmail;

                Variables varSubst = new Variables(Manager, Parameters);
                linesText = varSubst.ReplaceVariables(linesText);
                linesText = varSubst.ReplaceVariables(linesText);// twice, in case we have variables containing variables

                MailMessage = new MailMessage();
                MailMessage message = MailMessage;
                message.To.Add(toEmail);

                message.Subject = varSubst.ReplaceVariables(subject);
                message.From = new MailAddress(fromEmail);
                message.Body = linesText;

                // Add headers if available
                if (Headers != null)
                    message.Headers.Add(Headers);

                // add html if available
                if (!string.IsNullOrEmpty(linesHtml)) {

                    // Construct the alternate body as HTML.
                    linesHtml = varSubst.ReplaceVariables(linesHtml);
                    linesHtml = varSubst.ReplaceVariables(linesHtml);// twice, in case we have variables containing variables

                    MakeInlineFileItemsInfo fileInfo = new MakeInlineFileItemsInfo {
                        LinesHtml = linesHtml,
                        Files = new List<string>(),
                    };

                    int inlineIndex = 0;

                    List<LinkedResource> linkedResources = new List<LinkedResource>();
                    inlineIndex = await MakeInlineFileItemsAsync(linkedResources, inlineIndex, htmlFolder!, new Regex(@"url\(\s*file:///([^\)]*)\)"), fileInfo);
                    inlineIndex = await MakeInlineFileItemsAsync(linkedResources, inlineIndex, htmlFolder!, new Regex(@"=\""\s*file:///([^""]*)\"""), fileInfo);
                    inlineIndex = await MakeInlineFileItemsAsync(linkedResources, inlineIndex, htmlFolder!, new Regex(@"=\'\s*file:///([^']*)\'"), fileInfo);

                    //fileInfo = await MakeInlineUrlItemsAsync(new Regex(@"url\(\s*(http(s|)://[^\)]*)\)"), fileInfo);
                    //fileInfo = await MakeInlineUrlItemsAsync(new Regex(@"=\""\s*(http(s|)://[^""]*)\"""), fileInfo);
                    //fileInfo = await MakeInlineUrlItemsAsync(new Regex(@"=\'\s*(http(s|)://[^']*)\'"), fileInfo);
                    //fileInfo = await MakeInlineUrlItemsAsync(new Regex(@"=\'\s*(//[^']*)\'"), fileInfo);
                    //fileInfo = await MakeInlineUrlItemsAsync(new Regex(@"=\""\s*(//[^""]*)\"""), fileInfo);
                    inlineIndex = await MakeInlineUrlItemsAsync(linkedResources, inlineIndex, new Regex(@"url\(\s*(/FileHndlr\.image\?[^\)]*)\)"), fileInfo);
                    inlineIndex = await MakeInlineUrlItemsAsync(linkedResources, inlineIndex, new Regex(@"=\'\s*(/FileHndlr\.image\?[^']*)\'"), fileInfo);
                    inlineIndex = await MakeInlineUrlItemsAsync(linkedResources, inlineIndex, new Regex(@"=\""\s*(/FileHndlr\.image\?[^""]*)\"""), fileInfo);

                    ContentType mimeType = new ContentType("text/html");
                    AlternateView alternate = AlternateView.CreateAlternateViewFromString(fileInfo.LinesHtml, mimeType);
                    foreach (LinkedResource lr in linkedResources)
                        alternate.LinkedResources.Add(lr);
                    message.AlternateViews.Add(alternate);
                }
                SmtpClient = new SmtpClient(server, port) {
                    EnableSsl = ssl
                };
                if (auth == SMTPServer.AuthEnum.Anonymous) {
                    SmtpClient.Credentials = CredentialCache.DefaultNetworkCredentials;
                } else {
                    SmtpClient.Credentials = new NetworkCredential(username, password);
                }
            } catch (Exception exc) {
                Logging.AddErrorLog("Server={0}, SSL={1}, Auth={2}, UserName={3}, Password={4}, FromEmail={5}, ToEmail={6}, Subject={7}", server, ssl.ToString(), auth.ToString(), username, password, fromEmail, toEmail, subject, exc);
                throw;
            }
        }

        // find   ="file:///....." in text and replace with ="cid:C{1}"
        private class MakeInlineFileItemsInfo {
            public List<string> Files { get; set; } = null!;
            public string LinesHtml { get; set; } = null!;
        }
        private static async Task<int> MakeInlineUrlItemsAsync(List<LinkedResource> linkedResources, int inlineIndex, Regex regex, MakeInlineFileItemsInfo info) {
            Match m;
            int pos = 0;
            for (;;) {
                m = regex.Match(info.LinesHtml, pos);
                if (!m.Success)
                    break;

                string src = m.Groups[1].ToString();
                //src = InlineBaseFolder + src.Substring(InlineBaseSite.Length);
                string fullUrl = src;
                if (src.StartsWith("/")) {
                    fullUrl = Utility.HtmlDecode(fullUrl);
                    fullUrl = YetaWFManager.Manager.CurrentSite.MakeFullUrl(fullUrl);
                }

                string contentType;// a bit hacky
                if (src.ToLower().Contains(".jpeg") || src.ToLower().Contains(".jpg"))
                    contentType = "image/jpeg";
                else if (src.ToLower().Contains(".png"))
                    contentType = "image/png";
                else if (src.ToLower().Contains(".gif"))
                    contentType = "image/gif";
                else
                    continue;

                LinkedResource lr;
                try {
                    Stream stream;
                    if (YetaWFManager.IsSync())
                        stream = Client.GetStreamAsync(fullUrl).Result;
                    else
                        stream = await Client.GetStreamAsync(fullUrl);
                    lr = new LinkedResource(stream, contentType);
                } catch (Exception exc) {
                    throw new Error($"Can't retrieve {fullUrl} - {ErrorHandling.FormatExceptionMessage(exc)}");
                }

                if (lr != null) {
                    lr.ContentId = "C" + (1000 + inlineIndex).ToString();
                    lr.TransferEncoding = System.Net.Mime.TransferEncoding.Base64;
                    linkedResources.Add(lr);

                    string newStr = string.Format(@"=""cid:C{0}""", 1000 + inlineIndex);
                    info.LinesHtml = info.LinesHtml.Substring(0, m.Index) + newStr + info.LinesHtml.Substring(m.Index + m.Length);
                    pos = m.Index + newStr.Length;

                    ++inlineIndex;
                } else
                    pos = m.Index + m.Length;
            }
            return inlineIndex;
        }

        private static async Task<int> MakeInlineFileItemsAsync(List<LinkedResource> linkedResources, int inlineIndex, string htmlFolder, Regex regex, MakeInlineFileItemsInfo info) {
            Match m;
            int pos = 0;
            for (; ; ) {
                m = regex.Match(info.LinesHtml, pos);
                if (!m.Success)
                    break;
                string src = m.Groups[1].ToString();
                src = Utility.FileToPhysical(src);
                src = src.Replace("|", @":");
                if ((src.StartsWith(".\\") || src.StartsWith("./")) && !string.IsNullOrWhiteSpace(htmlFolder))
                    src = Path.Combine(htmlFolder, src.Substring(2));

                string contentType;
                if (src.ToLower().EndsWith(".jpeg") || src.ToLower().EndsWith(".jpg"))
                    contentType = "image/jpeg";
                else if (src.ToLower().EndsWith(".png"))
                    contentType = "image/png";
                else if (src.ToLower().EndsWith(".gif"))
                    contentType = "image/gif";
                else
                    continue;

                if (await FileSystem.FileSystemProvider.FileExistsAsync(src)) {
                    LinkedResource lr = new LinkedResource(src, contentType);
                    lr.ContentId = "C" + (1000 + inlineIndex).ToString();
                    lr.TransferEncoding = System.Net.Mime.TransferEncoding.Base64;
                    linkedResources.Add(lr);

                    string newStr = string.Format(@"=""cid:C{0}""", 1000 + info.Files.Count);
                    info.LinesHtml = info.LinesHtml.Substring(0, m.Index) + newStr + info.LinesHtml.Substring(m.Index + m.Length);
                    info.Files.Add(src);
                    pos = m.Index + newStr.Length;

                    ++inlineIndex;
                } else
                    pos = m.Index + m.Length;
            }
            return inlineIndex;
        }

        public void AddBcc(string ccEmail) {
            if (SendEmailProvider != null) {
                SendEmailProvider.AddBcc(SendEmailData, ccEmail);
            } else {
                if (!string.IsNullOrEmpty(ccEmail))
                    MailMessage.Bcc.Add(new MailAddress(ccEmail));
            }
        }

        public async Task SendAsync(bool fThrowError = true) {
            if (SendEmailProvider != null) {
                await SendEmailProvider.SendAsync(SendEmailData, fThrowError);
                SendingEmailAddress = await SendEmailProvider.GetSendingEmailAddressAsync(SendEmailData);
            } else {
                try {
                    if (YetaWFManager.IsSync()) {
                        SmtpClient.Send(MailMessage);
                    } else {
                        await SmtpClient.SendMailAsync(MailMessage);
                    }
                } catch (Exception exc) {
                    Logging.AddErrorLog("Server={0}, SSL={1}, Auth={2}", SmtpClient.Host, SmtpClient.EnableSsl.ToString(), SmtpClient.UseDefaultCredentials.ToString(), exc);
                    if (fThrowError)
                        throw;
                } finally {
                    SmtpClient.Dispose();
                    MailMessage.Dispose();
                }
            }
        }
    }
}
