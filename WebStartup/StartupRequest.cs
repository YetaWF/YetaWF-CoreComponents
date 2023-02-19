/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Builder;
using YetaWF.Core.DataProvider;
using YetaWF.Core.Log;
using YetaWF.Core.Packages;
using YetaWF.Core.Site;
using YetaWF.Core.Support;

namespace YetaWF.Core.WebStartup;

internal static class StartupRequest {

    private static readonly object _lockObject = new object();

    internal static IApplicationBuilder? StartYetaWF(this IApplicationBuilder? app) {

        if (!YetaWF.Core.Support.Startup.Started) {

            lock (_lockObject) { // protect from duplicate startup

                if (!YetaWF.Core.Support.Startup.Started) {

                    YetaWFManager.Syncify(async () => { // startup code

                        // Create a startup log file
                        StartupLogging startupLog = new StartupLogging();
                        await Logging.RegisterLoggingAsync(startupLog);

                        Logging.AddLog($"{nameof(StartYetaWF)} starting");

                        YetaWFManager manager = YetaWFManager.MakeInitialThreadInstance(new SiteDefinition() { SiteDomain = "__STARTUP" }, null, app.ApplicationServices); // while loading packages we need a manager
                        YetaWFManager.Syncify(async () => {
                            // External data providers
                            ExternalDataProviders.RegisterExternalDataProviders();
                            // Call all classes that expose the interface IInitializeApplicationStartup
                            await YetaWF.Core.Support.Startup.CallStartupClassesAsync();

                            if (!YetaWF.Core.Support.Startup.MultiInstance)
                                await Package.UpgradeToNewPackagesAsync();

                            YetaWF.Core.Support.Startup.Started = true;
                        });

                        // Stop startup log file
                        Logging.UnregisterLogging(startupLog);

                        // start real logging
                        await Logging.SetupLoggingAsync();

                        YetaWFManager.RemoveThreadInstance(); // Remove startup manager

                        Logging.AddLog($"{nameof(StartYetaWF)} completed");
                    });
                }
            }
        }
        return app;
    }
}
