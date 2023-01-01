/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using YetaWF.Core.Controllers;
using YetaWF.Core.Log;
using YetaWF.Core.Packages;
using YetaWF.Core.Site;
using YetaWF.Core.Support;

namespace YetaWF.Core {

    public interface ISignalRHub {
        void MapHub(IEndpointRouteBuilder bldr);
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

        public static void ConfigureHubs(IEndpointRouteBuilder endpoints) {
            // Find all hubs
            Logging.AddLog("Configuring SignalR hubs");
            List<Type> hubTypes = Package.GetClassesInPackages<ISignalRHub>();
            foreach (Type hubType in hubTypes) {
                Logging.AddLog($"Adding {hubType.FullName}");
                ISignalRHub hub = (ISignalRHub)Activator.CreateInstance(hubType)!;
                hub.MapHub(endpoints);
            }
            Logging.AddLog("Done configuring SignalR hubs");
        }

        public static string MakeUrl(string path) {
            return $"{SignalRUrl}/{path}";
        }

        /// <summary>
        /// Set up environment info for SignalR requests.
        /// </summary>
        /// <remarks>
        /// Any inbound signalr request must call this to set up the proper environment.
        /// Currently the implementation is a bit borked and the caller must wrap the code in a thread.
        /// We need to revisit this, but I don't have time to do it right, right now.
        /// </remarks>
        public static async Task SetupSignalRHubAsync(Hub hub, Func<Task> run) {

            HttpContext? httpContext = hub.Context.GetHttpContext();
            if (httpContext == null) throw new InternalError("No HTTP Context for this connection");
            HttpRequest httpReq = httpContext.Request;
            string host = httpReq.Host.Host;

            SiteDefinition? site = await SiteDefinition.LoadSiteDefinitionAsync(host);
            if (site == null) throw new InternalError($"No site definition for {host}");

            ExecuteParms parms = new ExecuteParms {
                HttpContext = httpContext,
                Run = run,
                Site = site,
            };
            Thread signalRThread = new Thread(new ParameterizedThreadStart(Execute));
            signalRThread.Start(parms);
            signalRThread.Join();
        }

        private static void Execute(object? p) {
            if (p == null) return;
            ExecuteParms parms = (ExecuteParms)p;
            YetaWFManager manager = YetaWFManager.MakeInitialThreadInstance(parms.Site, parms.HttpContext, true);
            if (manager != YetaWFManager.Manager)
                throw new InternalError("Mismatched Manager");
            YetaWFManager.Syncify(async () => {
                await YetaWFController.SetupEnvironmentInfoAsync();
                await parms.Run();
            });
            YetaWFManager.RemoveThreadInstance();
        }

        private class ExecuteParms {
            public HttpContext HttpContext { get; set; } = null!;
            public SiteDefinition Site { get; set; } = null!;
            public Func<Task> Run { get; set; } = null!;
        }

        /// <summary>
        /// Sets up SignalR use.
        /// </summary>
        /// <returns></returns>
        public static async Task UseAsync() {
            Package package = AreaRegistration.CurrentPackage;
            await YetaWFManager.Manager.AddOnManager.AddAddOnNamedAsync(package.AreaName, "github.com.signalr.signalr");
            YetaWFManager.Manager.ScriptManager.AddConfigOption("SignalR", "Url", SignalRUrl);

            YetaWFManager.Manager.ScriptManager.AddConfigOption("SignalR", "Version", "MVC6" );
        }
    }
}
