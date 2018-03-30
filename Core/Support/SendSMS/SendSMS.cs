/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YetaWF.Core.Controllers;
using YetaWF.Core.Localize;
using YetaWF.Core.Log;

namespace YetaWF.Core.Support.SendSMS {

    /// <summary>
    /// Defines all features an SMS provider provides to send SMS messages.
    /// </summary>
    public interface ISendSMS {
        /// <summary>
        /// Send an SMS message to a phone number.
        /// </summary>
        /// <param name="toNumber">The receiving number of the SMS message.</param>
        /// <param name="text">The text of the SMS message.</param>
        /// <param name="FromNumber">Optional sending number. If not specified, the SMS Settings are used to provide the default number.</param>
        Task SendSMSAsync(string toNumber, string text, string FromNumber = null);
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

    public class SendSMS {

        public const int MaxMessageLength = 918;

        private static List<ISendSMS> RegisteredProcessors = new List<ISendSMS>();

        /// <summary>
        /// Registers an SMS processor.
        /// </summary>
        public static void Register(ISendSMS iproc) {
            Logging.AddLog("Registering SMS processor named {0}", iproc.Name);
            RegisteredProcessors.Add(iproc);
        }
        public static async Task<ISendSMS> GetSMSProcessorAsync() {
            List<ISendSMS> procs = new List<ISendSMS>();
            foreach (ISendSMS r in procs) {
                if (await r.IsAvailableAsync())
                    procs.Add(r);
            }
            if (procs.Count() > 1)
                Logging.AddErrorLog("Multiple SMS processors are available - only the first one registered is used - {0}", procs.First().Name);
            else if (procs.Count() == 0)
                throw new InternalError("No SMS processor available");
            return procs.First();
        }
        public class GetSMSProcessorCondInfo {
            public ISendSMS Processor { get; set; }
            public int Count { get; set; }
        }
        public static async Task<GetSMSProcessorCondInfo> GetSMSProcessorCondAsync() {
            List<ISendSMS> procs = new List<ISendSMS>();
            foreach (ISendSMS r in procs) {
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
        /// <param name="toNumber">Specify the phone number of the recipient. If <i>toNumber</i> specifies an email address, the text is emailed to the specified email address.</param>
        /// <param name="text">The text of the SMS message.</param>
        /// <param name="FromNumber">Optional sending number. If not specified, the SMS Settings are used to provide the default number.</param>
        /// <remarks>
        /// This is the general method to call to send an SMS, whether a receiving phone number is specified or a receiving email address.
        /// This allows the same method to be used to send an SMS using a provider that supports an SMS to email gateway (often free). This should only be used
        /// to send SMS/emails to known email addresses (for example, to the YetaWF site administrator) and can't be used for general users.
        ///
        /// In order to use a phone number as <i>toNumber</i>, an SMS provider has to be installed. SMS providers offering SMS services are generally not free.
        /// </remarks>
        public async Task SendMessageAsync(string toNumber, string text, string FromNumber = null, bool ThrowError = true) {
            if (toNumber.Contains("@")) {
                // send email
                SendEmail.SendEmail sendEmail = new SendEmail.SendEmail();
                object parms = new {
                    Message = text,
                };
                await sendEmail.PrepareEmailMessageAsync(toNumber, this.__ResStr("smsSubject", "SMS"), await sendEmail.GetEmailFileAsync(AreaRegistration.CurrentPackage, "Text Message.txt"), parameters: parms);
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
