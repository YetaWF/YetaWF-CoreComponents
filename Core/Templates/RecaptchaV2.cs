/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Threading.Tasks;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;
using YetaWF.Core.Views.Shared;

namespace YetaWF.Core.Addons.Templates {
    public class RecaptchaV2 : IAddOnSupport {

        public async Task AddSupportAsync(YetaWFManager manager) {

            RecaptchaV2Config config = await RecaptchaV2Config.LoadRecaptchaV2ConfigAsync();
            if (string.IsNullOrWhiteSpace(config.PublicKey))
                throw new InternalError("The Recaptcha configuration settings are missing - no public key found");

            ScriptManager scripts = manager.ScriptManager;
            scripts.AddConfigOption("RecaptchaV2", "SiteKey", config.PublicKey);
            scripts.AddConfigOption("RecaptchaV2", "Theme", config.GetTheme());
            scripts.AddConfigOption("RecaptchaV2", "Size", config.GetSize());
        }
    }
}
