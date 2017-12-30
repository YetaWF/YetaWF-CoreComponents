/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

#if MVC6
#else

using Microsoft.AspNet.SignalR;
using Owin;

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
    }

}

#endif
