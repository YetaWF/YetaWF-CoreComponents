/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Threading.Tasks;
using YetaWF.Core.Controllers;
using YetaWF.Core.Site;
#if MVC6
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
#else
using Microsoft.AspNet.SignalR;
using Owin;
using System.Web;
#endif

namespace YetaWF.Core.Support {

    public class Signalr
#if MVC6
#else
        : IInitializeOwinStartup 
#endif
    {
        public static readonly string SignalRUrl = "/__signalr";

        public static async Task UseAsync() {
            await YetaWFManager.Manager.AddOnManager.AddAddOnGlobalAsync("github.com.signalr", "signalr");
            YetaWFManager.Manager.ScriptManager.AddConfigOption("Basics", "SignalRUrl", SignalRUrl);
        }

#if MVC6
        public static void ConfigureServices(IServiceCollection services) {
            services.AddSignalR();
        }

        public static void ConfigureHubs(IApplicationBuilder app) {
            // Find all hubs
            //**TODO:
            // This is where I stopped as there are quite a few missing pieces (1/14/2018, like Others, Caller, etc.) - As I don't
            // currently need signalr support on asp.net core, we'll just wait. https://github.com/aspnet/SignalR/issues/394

            app.UseSignalR((routes) => {
               //TODO: routes.MapHub<xxx>(SignalRUrl + "/" + "xxxx");
            });
        }
#else
        public void InitializeOwinStartup(IAppBuilder app) {
            HubConfiguration hubConfig = new HubConfiguration();
#if DEBUG
            hubConfig.EnableDetailedErrors = true;
#endif
            hubConfig.EnableJavaScriptProxies = false;
            app.MapSignalR(SignalRUrl, hubConfig);
        }
#endif

        /// <summary>
        /// Set up environment info for SignalR requests.
        /// </summary>
        public static async Task<YetaWFManager> SetupEnvironmentAsync() {
            if (YetaWFManager.HaveManager) return YetaWFManager.Manager;
            YetaWFManager manager;
            string host;
#if MVC6
            HttpContext context = YetaWFManager.HttpContextAccessor.HttpContext;
            HttpRequest httpReq = context.Request;
            host = httpReq.Host.Host;
            manager = YetaWFManager.MakeInstance(context, host);
#else
            HttpRequest httpReq = HttpContext.Current.Request;
            host = httpReq.Url.Host;
            manager = YetaWFManager.MakeInstance(host);
#endif
            manager.CurrentSite = await SiteDefinition.LoadSiteDefinitionAsync(host);
            if (manager.CurrentSite == null) throw new InternalError("No site definition for {0}", host);
            await YetaWFController.SetupEnvironmentInfoAsync();

            return manager;
        }
    }
}
