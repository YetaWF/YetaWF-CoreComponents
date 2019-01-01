using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YetaWF.Core.Controllers;
using YetaWF.Core.Packages;
using YetaWF.Core.Support;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using YetaWF.Core.Log;
using YetaWF.Core.Site;
#if MVC6
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
#else
#endif

namespace YetaWF.Core {

    public interface ISignalRHub {
        void AddToRouteMap(HubRouteBuilder routes);
    }

    public static class SignalR {

        public static readonly string SignalRUrl = "/__signalr";

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

        public static string MakeUrl(Package package, string path) {
            return $"{SignalRUrl}/{package.AreaName}/{path}";
        }

        /// <summary>
        /// Set up environment info for SignalR requests.
        /// </summary>
        public static async Task<YetaWFManager> SetupSignalRAsync(this Hub hub) {

            YetaWFManager manager;
#if MVC6
            HttpContext httpContext = hub.Context.GetHttpContext();
            if (!YetaWFManager.HaveManager) {
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
            YetaWFManager.Manager.ScriptManager.AddConfigOption(package.Domain, "SignalRUrl", SignalRUrl);
        }
    }
    //public class TestHub : Hub, ISignalRHub {
    //    public void AddToRouteMap(HubRouteBuilder routes) {
    //        routes.MapHub<TestHub>(SignalR.MakeUrl(AreaRegistration.CurrentPackage, "something"));
    //    }
    //}
}
