/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text.RegularExpressions;
using YetaWF.Core.Addons;
using YetaWF.Core.Localize;
using YetaWF.Core.Log;
using YetaWF.Core.Packages;
using YetaWF.Core.Support;

namespace YetaWF.Core.SendEmail {

    public class SendEmail {

        public const string EmailsFolder = "Emails";
        public const string EmailHtmlExtension = ".html";
        public const string EmailTxtExtension = ".txt";

        public SendEmail() { }

        protected YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        protected MailMessage MailMessage { get; set; }
        protected SmtpClient SmtpClient { get; set; }

        public void PrepareEmailMessageFromStrings(string toEmail, string subject, string emailText, string emailHTML, string fomEmail = null, object parameters = null) {
            Manager.CurrentSite.SMTP.Validate();
            SMTPServer smtpEmail = Manager.CurrentSite.SMTP;
            emailHTML = "<!DOCTYPE html><html><head>" +
                "<title>" + subject + "</title>" +
                "</head><body style='margin:0'>" + emailHTML + "</body></html>";
            PrepareEmailMessage(smtpEmail.Server, smtpEmail.Port, smtpEmail.SSL, smtpEmail.Authentication, smtpEmail.UserName, smtpEmail.Password, null, toEmail, subject, emailText, emailHTML, null, parameters);
        }
        public string GetEmailFile(Package package, string filename) {
            string moduleAddOnUrl = VersionManager.GetAddOnModuleUrl(package.Domain, package.Product);
            string customModuleAddOnUrl = VersionManager.GetCustomUrlFromUrl(moduleAddOnUrl);

            // locate site specific custom email
            string file = YetaWFManager.UrlToPhysical(string.Format("{0}/{1}/{2}", customModuleAddOnUrl, EmailsFolder, filename));
            if (System.IO.File.Exists(file))
                return file;
            // locate (not site specific) custom email
            file = YetaWFManager.UrlToPhysical(string.Format("{0}/{1}/{2}", customModuleAddOnUrl, EmailsFolder, filename));
            if (System.IO.File.Exists(file))
                return file;
            // otherwise use default email
            file = YetaWFManager.UrlToPhysical(string.Format("{0}/{1}/{2}", moduleAddOnUrl, EmailsFolder, filename));
            if (System.IO.File.Exists(file))
                return file;
            throw new InternalError("Email configuration file {0} not found at {1}", filename, moduleAddOnUrl);
        }
        public void PrepareEmailMessage(string toEmail, string subject, string emailFile, string fromEmail = null, object parameters = null) {
            Manager.CurrentSite.SMTP.Validate();
            SMTPServer smtpEmail = Manager.CurrentSite.SMTP;
            PrepareEmailMessage(smtpEmail.Server, smtpEmail.Port, smtpEmail.SSL, smtpEmail.Authentication, smtpEmail.UserName, smtpEmail.Password, fromEmail, toEmail, subject, emailFile, parameters);
        }
        public void PrepareEmailMessage(string server, int port, bool ssl, SMTPServer.AuthEnum auth, string username, string password, string fromEmail, string toEmail, string subject, string emailFile, object parameters = null) {
            string file = emailFile;
            if (!file.EndsWith(EmailTxtExtension, StringComparison.CurrentCultureIgnoreCase))
                throw new Error(this.__ResStr("errEmailTextInv", "The base email file {0} must be a text file (ending in .txt)"), file);

            Logging.AddLog("Sending email {0} to {1}", emailFile, toEmail);

            // read simple txt
            string linesText;
            try {
                linesText = File.ReadAllText(file);
            } catch (Exception exc) {
                throw new Error(this.__ResStr("errEmailTextFile", "The email {0} could not be found."), file, exc);
            }

            // add html formatted file if available
            file = Path.ChangeExtension(emailFile, EmailHtmlExtension);
            string linesHtml = null, htmlFolder = null;
            try {
                linesHtml = File.ReadAllText(file);
                htmlFolder = Path.GetDirectoryName(file);
            } catch (Exception exc) {
                if (!(exc is FileNotFoundException))
                    Logging.AddErrorLog(this.__ResStr("errEmailHtmlFile", "The html formatted email {0} could not be read."), file, exc);
                linesHtml = null;
                htmlFolder = null;
            }
            PrepareEmailMessage(server, port, ssl, auth, username, password, fromEmail, toEmail, subject, linesText, linesHtml, htmlFolder, parameters);
        }
        protected void PrepareEmailMessage(string server, int port, bool ssl, SMTPServer.AuthEnum auth, string username, string password, string fromEmail, string toEmail, string subject, string linesText, string linesHtml, string htmlFolder, object parameters = null) {
            try {
                if (string.IsNullOrEmpty(server))
                    throw new Error(this.__ResStr("errMailServer", "No mail server specified."));

                if (string.IsNullOrEmpty(toEmail) || Manager.CurrentSite.EmailDebug) // force email address)
                    toEmail = Manager.CurrentSite.AdminEmail;
                if (string.IsNullOrEmpty(toEmail))
                    throw new Error(this.__ResStr("errNoRecv", "No receiving email address specified - The site administrator's email address is used as receiving email address - The site administrator's email address can be defined using the Site Properties"));
                if (string.IsNullOrEmpty(fromEmail))
                    fromEmail = Manager.CurrentSite.AdminEmail;
                if (string.IsNullOrEmpty(fromEmail))
                    throw new Error(this.__ResStr("errNoSender", "No sending email address specified"));

                Variables varSubst = new Variables(Manager, parameters);
                linesText = varSubst.ReplaceVariables(linesText);
                linesText = varSubst.ReplaceVariables(linesText);// twice, in case we have variables containing variables

                MailMessage = new MailMessage();
                MailMessage message = MailMessage;
                message.To.Add(toEmail);

                message.Subject = varSubst.ReplaceVariables(subject);
                message.From = new MailAddress(fromEmail);
                message.Body = linesText;

                // add html if available
                if (!string.IsNullOrEmpty(linesHtml)) {

                    // Construct the alternate body as HTML.
                    linesHtml = varSubst.ReplaceVariables(linesHtml);
                    linesHtml = varSubst.ReplaceVariables(linesHtml);// twice, in case we have variables containing variables

                    List<string> files = new List<string>(); // inline images
                    linesHtml = MakeInlineItems(linesHtml, htmlFolder, new Regex(@"=\""\s*file:///([^""]*)\"""), ref files);
                    linesHtml = MakeInlineItems(linesHtml, htmlFolder, new Regex(@"=\'\s*file:///([^']*)\'"), ref files);

                    ContentType mimeType = new ContentType("text/html");
                    AlternateView alternate = AlternateView.CreateAlternateViewFromString(linesHtml, mimeType);
                    message.AlternateViews.Add(alternate);

                    // add inline images as attachments
                    int i = 0;
                    foreach (var file in files) {
                        LinkedResource lr;
                        if (file.ToLower().EndsWith(".jpeg") || file.ToLower().EndsWith(".jpg"))
                            lr = new LinkedResource(file, "image/jpeg");
                        else if (file.ToLower().EndsWith(".png"))
                            lr = new LinkedResource(file, "image/png");
                        else if (file.ToLower().EndsWith(".gif"))
                            lr = new LinkedResource(file, "image/gif");
                        else {
                            continue;
                        }
                        lr.ContentId = "C" + (1000 + i).ToString();
                        lr.TransferEncoding = System.Net.Mime.TransferEncoding.Base64;
                        try {
                            alternate.LinkedResources.Add(lr);
                        } catch { }
                        ++i;
                    }
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
        private static string MakeInlineItems(string linesHtml, string htmlFolder, Regex regex, ref List<string> files) {

            Match m;
            int pos = 0;
            for (;;) {
                m = regex.Match(linesHtml, pos);
                if (!m.Success)
                    break;

                string src = m.Groups[1].ToString();
                //src = InlineBaseFolder + src.Substring(InlineBaseSite.Length);
                src = src.Replace("/", @"\").Replace("|", @":");
                if (src.StartsWith(".\\") && !string.IsNullOrWhiteSpace(htmlFolder)) {
                    src = Path.Combine(htmlFolder, src.Substring(2));
                }
                if (File.Exists(src)) {
                    string newStr = string.Format(@"=""cid:C{0}""", 1000 + files.Count);
                    linesHtml = linesHtml.Substring(0, m.Index) + newStr + linesHtml.Substring(m.Index + m.Length);
                    files.Add(src);
                    pos = m.Index + newStr.Length;
                } else
                    pos = m.Index + m.Length;
            }
            return linesHtml;
        }

        public void AddBcc(string ccEmail) {
            if (!string.IsNullOrEmpty(ccEmail))
                MailMessage.Bcc.Add(new MailAddress(ccEmail));
        }

        public void Send(bool fThrowError = true) {
            try {
                SmtpClient.Send(MailMessage);
            } catch (Exception exc) {
                Logging.AddErrorLog("Server={0}, SSL={1}, Auth={2}", SmtpClient.Host, SmtpClient.EnableSsl.ToString(), SmtpClient.UseDefaultCredentials.ToString(), exc);
                if (fThrowError)
                    throw;
            } finally {
                SmtpClient.Dispose();
            }
        }
    }
}
