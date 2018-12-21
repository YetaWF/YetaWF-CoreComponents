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

    public class SignalR {

        public static readonly string SignalRUrl = "/__signalr";

        public static void ConfigureServices(IServiceCollection services) {
            services.AddSignalR();
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
