/* Copyright © 2022 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YetaWF.Core.Localize;
using YetaWF.Core.Log;

namespace YetaWF.Core.Support.SendSMS {

    /// <summary>
    /// Defines all features an SMS provider provides to send SMS messages.
    /// Applications don't need to use this interface directly.
    /// To send text messages, use the SendSMS.SendMessageAsync() method, which automatically handles retrieving the SMS provider.
    /// </summary>
    public interface ISendSMS {
        /// <summary>
        /// Send an SMS message to a phone number.
        /// </summary>
        /// <param name="toNumber">The receiving number of the SMS message.</param>
        /// <param name="text">The text of the SMS message.</param>
        /// <param name="FromNumber">Optional sending number. If not specified, the SMS Settings are used to provide the default number.</param>
        Task SendSMSAsync(string toNumber, string text, string? FromNumber = null);
        /// <summary>
        /// The name of the SMS provider.
        /// </summary>
        string Name { get; }
        /// <summary>
        /// Returns whether the SMS provider is available.
        /// </summary>
        /// <returns>True if it is available, false otherwise.</returns>
        Task<bool> IsAvailableAsync();
        /// <summary>
        /// Returns whether the SMS provider is in test mode.
        /// </summary>
        /// <returns>True if it is in test mode, false otherwise.</returns>
        Task<bool> IsTestModeAsync();
    }

    /// <summary>
    /// Used to send SMS text messages and manages all SMS providers.
    /// To send text messages, use the SendSMS.SendMessageAsync() method, which automatically handles retrieving the SMS provider.
    /// </summary>
    public class SendSMS {

        /// <summary>
        /// Defines the maximum length of a text message.
        /// </summary>
        public const int MaxMessageLength = 918;

        private static List<ISendSMS> RegisteredProcessors = new List<ISendSMS>();

        /// <summary>
        /// Registers an SMS provider.
        /// </summary>
        public static void Register(ISendSMS iproc) {
            Logging.AddLog("Registering SMS provider named {0}", iproc.Name);
            RegisteredProcessors.Add(iproc);
        }
        /// <summary>
        /// Retrieves the installed SMS provider.
        /// </summary>
        /// <returns>Returns a ISendSMS interface provided by the SMS provider.</returns>
        /// <remarks>There is only one active SMS provider.
        ///
        /// If no SMS provider is available, an error exception occurs.</remarks>
        public static async Task<ISendSMS> GetSMSProcessorAsync() {
            List<ISendSMS> procs = new List<ISendSMS>();
            foreach (ISendSMS r in RegisteredProcessors) {
                if (await r.IsAvailableAsync())
                    procs.Add(r);
            }
            if (procs.Count() > 1)
                Logging.AddErrorLog("Multiple SMS providers are available - only the first one registered is used - {0}", procs.First().Name);
            else if (procs.Count() == 0)
                throw new InternalError("No SMS provider available");
            return procs.First();
        }
        /// <summary>
        /// Defines the object returned by the GetSMSProcessorCondAsync method.
        /// </summary>
        public class GetSMSProcessorCondInfo {
            /// <summary>
            /// The ISendSMS interface provided by the SMS provider, or null if no SMS provider is available.
            /// </summary>
            public ISendSMS? Processor { get; set; }
            /// <summary>
            /// The total number of SMS providers installed. Only one SMS provider can be active.
            /// </summary>
            public int Count { get; set; }
        }
        /// <summary>
        /// Retrieves the installed SMS provider.
        /// </summary>
        /// <returns>Returns information about the installed SMS provider, if any.</returns>
        /// <remarks>There is only one active SMS provider.</remarks>
        public static async Task<GetSMSProcessorCondInfo> GetSMSProcessorCondAsync() {
            List<ISendSMS> procs = new List<ISendSMS>();
            foreach (ISendSMS r in RegisteredProcessors) {
                if (await r.IsAvailableAsync())
                    procs.Add(r);
            }
            int count = procs.Count();
            return new GetSMSProcessorCondInfo {
                Processor = count > 0 && procs != null ? procs.First() : null,
                Count = count,
            };
        }

        /// <summary>
        /// Sends an SMS message to a phone number or emails the specified text.
        /// </summary>
        /// <param name="toNumber">Specify the phone number of the recipient. If <paramref name="toNumber"/> specifies an email address, the text is emailed to the specified email address.</param>
        /// <param name="text">The text of the SMS message.</param>
        /// <param name="FromNumber">Optional sending number. If not specified, the SMS Settings are used to provide the default number.</param>
        /// <param name="ThrowError">If true is specified, an error exception is thrown if an error occurs sending the message, otherwise the method returns without any error indication.</param>
        /// <remarks>
        /// This is the general method to send an SMS, whether a receiving phone number is specified or a receiving email address.
        /// This allows the same method to be used to send an SMS using a provider that supports an SMS to email gateway (often free). This should only be used
        /// to send SMS/emails to known email addresses (for example, to the YetaWF site administrator) and can't be used for general users.
        ///
        /// In order to use a phone number as <paramref name="toNumber"/>, an SMS provider has to be installed. SMS providers offering SMS services are generally not free.
        /// When a phone number is used, it must be in E164 format (e.g., +14075551212).
        /// </remarks>
        public async Task SendMessageAsync(string toNumber, string text, string? FromNumber = null, bool ThrowError = true) {
            if (toNumber.Contains("@")) {
                // send email
                SendEmail.SendEmail sendEmail = new SendEmail.SendEmail();
                object parms = new {
                    Message = text,
                };
                await sendEmail.PrepareEmailMessageAsync(toNumber, this.__ResStr("smsSubject", "SMS"), await sendEmail.GetEmailFileAsync(AreaRegistration.CurrentPackage, "Text Message.txt"), Parameters: parms);
                await sendEmail.SendAsync(ThrowError);
            } else {
                // send using SMS provider
                ISendSMS sendSMS = await GetSMSProcessorAsync();
                if (sendSMS == null) {
                    if (ThrowError)
                        throw new InternalError("No SMS provider installed");
                    else
                        return;
                }
                try {
                    await sendSMS.SendSMSAsync(toNumber, text, FromNumber: FromNumber);
                } catch (Exception) {
                    if (ThrowError)
                        throw;
                }
            }
            Logging.AddLog("SMS sent to {0} - {1}", toNumber, text);
        }
    }
}
