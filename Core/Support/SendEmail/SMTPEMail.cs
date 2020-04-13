/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Newtonsoft.Json;
using YetaWF.Core.Controllers;
using YetaWF.Core.Localize;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Modules;
using YetaWF.Core.Support;
using static YetaWF.Core.Models.Attributes.ExprAttribute;

namespace YetaWF.Core.SendEmail {

    public class SMTPServer {

        public const int MaxServer = 100;
        public const int MaxUser = 100;
        public const int MaxPswd = 100;

        public SMTPServer() {
            Port = 25;
        }

        public bool IsValid {
            get {
                return !string.IsNullOrWhiteSpace(Server);
            }
        }
        public void Validate() {
            if (!IsValid)
                throw new Error(this.__ResStr("serverInvalid", "The email server has not been configured."));
        }

        public enum AuthEnum {
            [EnumDescription("Anonymous", "Anonymous login to mail server (not recommended)")]
            Anonymous=0,
            [EnumDescription("Signon", "Log into mail server with user name and password")]
            Signon=1,
        }

        [Caption("Server"), Description("The SMTP mail server used for all emails originating from this site")]
        [UIHint("Text40"), StringLength(MaxServer), Trim]
        public string Server { get; set; }

        [Caption("Port"), Description("The SMTP mail server port used (25 is usually the default)")]
        [UIHint("IntValue6"), Range(0, 999999), ProcessIfSupplied(nameof(Server)), RequiredIfSuppliedAttribute(nameof(Server)), Trim]
        public int Port { get; set; }

        [Caption("Authentication"), Description("Defines how the mail server is accessed to send emails. Most mail servers require authentication using a user name and password")]
        [UIHint("Enum"), ProcessIfSupplied(nameof(Server)), RequiredIfSuppliedAttribute(nameof(Server))]
        public AuthEnum Authentication { get; set; }

        [Caption("User Name"), Description("The user name used to log into the mail server when authentication is required by the mail server")]
        [UIHint("Text80"), StringLength(MaxUser), ProcessIf(nameof(Authentication), AuthEnum.Signon), RequiredIf(nameof(Authentication), AuthEnum.Signon), Trim]
        public string UserName { get; set; }

        [Caption("Password"), Description("The password used to log into the mail server when authentication is required by the mail server")]
        [UIHint("Password20"), StringLength(MaxPswd), ProcessIf(nameof(Authentication), AuthEnum.Signon), RequiredIf(nameof(Authentication), AuthEnum.Signon)]
        public string Password { get; set; }

        [Caption("Secure"), Description("Defines whether SSL is used when sending emails")]
        [UIHint("Boolean"), ProcessIfSupplied(nameof(Server))]
        public bool SSL { get; set; }

        [Caption("Send Test Email"), Description("Click to send a test email using the server information")]
        [UIHint("ModuleAction"), ReadOnly]
        [Expr(OpEnum.ProcessIf, nameof(Server), OpCond.NotEq, null, nameof(Port), OpCond.NotEq, null, nameof(Authentication), OpCond.Eq, AuthEnum.Anonymous)]
        [Expr(OpEnum.ProcessIf, nameof(Server), OpCond.NotEq, null, nameof(Port), OpCond.NotEq, null, nameof(Authentication), OpCond.Eq, AuthEnum.Signon, nameof(UserName), OpCond.NotEq, null, nameof(Password), OpCond.NotEq, null)]
        [JsonIgnore] // so it's not saved when json serializing site properties
        public ModuleAction SendTestEmail {
            get {
                YetaWFManager manager = YetaWFManager.Manager;
                if (!manager.HaveUser) return null;
                string userName = manager.UserEmail;
                return new ModuleAction {
                    Url = Utility.UrlFor(typeof(SMTPEmailController), nameof(SMTPEmailController.SendTestEmail)),
                    LinkText = this.__ResStr("send", "Send"),
                    Category = ModuleAction.ActionCategoryEnum.Update,
                    ConfirmationText = this.__ResStr("confirmSend", "Are you sure you want to send a test email to {0} using the provided server information?", userName),
                    Mode = ModuleAction.ActionModeEnum.Any,
                    Style = ModuleAction.ActionStyleEnum.Post,
                    Tooltip = this.__ResStr("sendTT", "Click to send a test email"),
                };
            }
        }
    }
}


