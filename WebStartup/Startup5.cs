/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

#if MVC6
#else

using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using YetaWF.Core.DataProvider;
using YetaWF.Core.Log;
using YetaWF.Core.Packages;
using YetaWF.Core.Site;
using YetaWF.Core.Support;

[assembly: WebActivatorEx.PostApplicationStartMethod(typeof(YetaWF.WebStartup.StartupMVC5), nameof(YetaWF.WebStartup.StartupMVC5.Start))]

namespace YetaWF.Core.WebStartup {

    public static class StartupMVC5 {

        public static void Start() {

            YetaWFManager manager = YetaWFManager.MakeInstance(HttpContext.Current, "__STARTUP"); // while loading packages we need a manager

            YetaWFManager.Syncify(async () => { // Startup, so async is pointless

                manager.CurrentSite = new SiteDefinition();

                // Create a startup log file
                StartupLogging startupLog = new StartupLogging();
                await Logging.RegisterLoggingAsync(startupLog);

                Logging.AddLog("{nameof(StartupMVC5)}.{nameof(Start)} starting");

                RouteTable.Routes.IgnoreRoute("FileHndlr.image/{*pathInfo}");

                Logging.AddLog("Calling AreaRegistration.RegisterAllAreas()");
                AreaRegistration.RegisterAllAreas();
                Logging.AddLog("Adding filters");
                //GlobalFilters.Filters.Add(new HandleErrorAttribute());

                Logging.AddLog("Adding catchall route");
                RouteTable.Routes.MapRoute(
                    "Page",
                    "{*__path}",
                    new { controller = "Page", action = "Show" },
                    new string[] { "YetaWF.Core.Controllers", } // namespace
                );

                // External data providers
                ExternalDataProviders.RegisterExternalDataProviders();
                // Call all classes that expose the interface IInitializeApplicationStartup
                await YetaWF.Core.Support.Startup.CallStartupClassesAsync();

                // Stop startup log file
                Logging.UnregisterLogging(startupLog);

                await Logging.SetupLoggingAsync();

                if (!YetaWF.Core.Support.Startup.MultiInstance)
                    await Package.UpgradeToNewPackagesAsync();

                YetaWF.Core.Support.Startup.Started = true;
                Logging.AddLog($"{nameof(StartupMVC5)}.{nameof(Start)} completed");
            });
        }
    }
}

#endif
