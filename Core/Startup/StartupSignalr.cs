/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

#if MVC6
#else

using Microsoft.AspNet.SignalR;
using Owin;
using System.Web;
using YetaWF.Core.Controllers;
using YetaWF.Core.Site;

namespace YetaWF.Core.Support {

    public class Signalr : IInitializeOwinStartup {

        public static readonly string SignalRUrl = "/__signalr";

        public static void Use() {
            YetaWFManager.Manager.AddOnManager.AddAddOnGlobal("github.com.signalr", "signalr");
            YetaWFManager.Manager.ScriptManager.AddConfigOption("Basics", "SignalRUrl", SignalRUrl);
        }

        public void InitializeOwinStartup(IAppBuilder app) {
            HubConfiguration hubConfig = new HubConfiguration();
#if DEBUG
            hubConfig.EnableDetailedErrors = true;
#endif
            hubConfig.EnableJavaScriptProxies = false;
            app.MapSignalR(SignalRUrl, hubConfig);
        }

        public static YetaWFManager SetupEnvironment() {
            if (YetaWFManager.HaveManager) return YetaWFManager.Manager;
            HttpRequest httpReq = HttpContext.Current.Request;
            string host = httpReq.Url.Host;
            YetaWFManager manager = YetaWFManager.MakeInstance(host);
            manager.CurrentSite = SiteDefinition.LoadSiteDefinition(host);
            if (manager.CurrentSite == null) throw new InternalError("No site definition for {0}", host);
            YetaWFController.SetupEnvironmentInfo();
            return manager;
        }
    }
}

#endif
