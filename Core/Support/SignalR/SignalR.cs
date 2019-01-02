/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YetaWF.Core.Controllers;
using YetaWF.Core.Packages;
using YetaWF.Core.Support;
using YetaWF.Core.Log;
using YetaWF.Core.Site;
#if MVC6
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
#else
using Owin;
using Microsoft.AspNet.SignalR;
using System.Web;
#endif

namespace YetaWF.Core {

    public interface ISignalRHub { // not really needed for mvc5
#if MVC6
        void AddToRouteMap(HubRouteBuilder routes);
#endif
    }

    public static class SignalR {

        public static readonly string SignalRUrl = "/__signalr";

#if MVC6
        public static void ConfigureServices(IServiceCollection services) {
            services.AddSignalR(hubOptions =>
            {
#if DEBUG
                hubOptions.EnableDetailedErrors = true;
#endif
            });
        }

        public static void ConfigureHubs(IApplicationBuilder app) {
            // Find all hubs
            Logging.AddLog("Configuring SignalR hubs");
            List<Type> hubTypes = Package.GetClassesInPackages<ISignalRHub>();
            foreach (Type hubType in hubTypes) {
                Logging.AddLog($"Adding {hubType.FullName}");
                app.UseSignalR((routes) => {
                    ISignalRHub hub = (ISignalRHub)Activator.CreateInstance(hubType);
                    hub.AddToRouteMap(routes);
                });
            }
            Logging.AddLog("Done configuring SignalR hubs");
        }
#else
        // MVC5 is initialized in the Messenger package. OwinStartup is based on package service level, so we can't init in YetaWF.Core, because
        // that is initialized before Identity. If SignalR is started before Identity, authentication in SignalR won't work.
#endif

        public static string MakeUrl(string path) {
            return $"{SignalRUrl}/{path}";
        }

        /// <summary>
        /// Set up environment info for SignalR requests.
        /// </summary>
        public static async Task<YetaWFManager> SetupSignalRAsync(this Hub hub) {

            YetaWFManager manager;
#if MVC6
            if (!YetaWFManager.HaveManager) {
                HttpContext httpContext = hub.Context.GetHttpContext();
                HttpRequest httpReq = httpContext.Request;
                string host = httpReq.Host.Host;
                YetaWFManager.MakeInstance(httpContext, host);
            }
#else
            if (!YetaWFManager.HaveManager) {
                HttpRequest httpReq = HttpContext.Current.Request;
                string host = httpReq.Url.Host;
                YetaWFManager.MakeInstance(host);
            }
#endif
            manager = YetaWFManager.Manager;

            manager.CurrentSite = await SiteDefinition.LoadSiteDefinitionAsync(manager.SiteDomain);
            if (manager.CurrentSite == null) throw new InternalError("No site definition for {0}", manager.SiteDomain);
            await YetaWFController.SetupEnvironmentInfoAsync();

            return manager;
        }

        /// <summary>
        /// Sets up SignalR use.
        /// </summary>
        /// <returns></returns>
        public static async Task UseAsync() {
            Package package = AreaRegistration.CurrentPackage;
            await YetaWFManager.Manager.AddOnManager.AddAddOnNamedAsync(package.AreaName, "github.com.signalr.signalr");
            YetaWFManager.Manager.ScriptManager.AddConfigOption("SignalR", "Url", SignalRUrl);

            YetaWFManager.Manager.ScriptManager.AddConfigOption("SignalR", "Version",
#if MVC6
                "MVC6"
#else
                "MVC5"
#endif
            );
        }
    }
}
