/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using System;
using System.IO;
using YetaWF.Core.DataProvider;
using YetaWF.Core.Log;
using YetaWF.Core.Site;
using YetaWF.Core.Support;

namespace YetaWF.Core.WebAPIStartup;

/// <summary>
/// The class implementing YetaWF API service requests.
/// </summary>
public static class StartupRequest {

    static SiteDefinition? CurrentSite = null;

    /// <summary>
    /// Processes an HTTP request (startup, initial processing).
    /// </summary>
    /// <param name="httpContext">The HttpContent instance.</param>
    public static void StartRequest(HttpContext httpContext) {

        // all code here is synchronous until a Manager is available.

        HttpRequest httpReq = httpContext.Request;
        Uri uri = new Uri(UriHelper.GetDisplayUrl(httpReq));
        Logging.AddLog(uri.ToString());

        // We have a valid request for the default domain
        // create a YetaWFManager object to keep track of everything (it serves
        // as a global anchor for everything we need to know while processing this request)

        YetaWFManager manager = YetaWFManager.MakeInstance(httpContext, CurrentSite!.SiteDomain);

        // Site properties are ONLY valid AFTER this call to YetaWFManager.MakeInstance

        manager.CurrentSite = CurrentSite;
        manager.IsStaticSite = false;
        manager.IsTestSite = false;
        manager.IsLocalHost = uri.IsLoopback;

        manager.HostUsed = uri.Host;
        manager.HostPortUsed = uri.Port;
        manager.HostSchemeUsed = uri.Scheme;

        UriBuilder uriBuilder = new UriBuilder(uri);
        uriBuilder.Scheme = manager.HostSchemeUsed;
        uriBuilder.Port = manager.HostPortUsed;
        uriBuilder.Host = manager.HostUsed;
        manager.CurrentRequestUrl = uriBuilder.ToString();
    }

    private static object _lockObject = new object();

    /// <summary>
    /// First time YetaWF API service startup processing.
    /// </summary>
    public static void StartYetaWF(this IApplicationBuilder app) {

        if (!YetaWF.Core.Support.Startup.Started) {

            lock (_lockObject) { // protect from duplicate startup

                if (!YetaWF.Core.Support.Startup.Started) {

                    YetaWFManager.Syncify(async () => { // startup code

                        YetaWFManager.Mode = YetaWFManager.SERVICEMODE;

                        // Create a startup log file
                        StartupLogging startupLog = new StartupLogging();
                        await Logging.RegisterLoggingAsync(startupLog);

                        Logging.AddLog($"{nameof(StartYetaWF)} starting");

                        YetaWFManager manager = YetaWFManager.MakeInitialThreadInstance(new SiteDefinition() { SiteDomain = YetaWFManager.SERVICEMODE }, null, app.ApplicationServices); // while loading packages we need a manager
                        YetaWFManager.Syncify(async () => {
                            // External data providers
                            ExternalDataProviders.RegisterExternalDataProviders();
                            // Call all classes that expose the interface IInitializeApplicationStartup
                            await YetaWF.Core.Support.Startup.CallStartupClassesAsync();

                            // Get default site
                            CurrentSite = null;
                            // try load from db
                            if (CurrentSite == null) {
                                if (SiteDefinition.LoadSiteDefinitionAsync != null) {
                                    // Requires YetaWF.SitePropertiesService if used
                                    CurrentSite = await SiteDefinition.LoadSiteDefinitionAsync(null);
                                }
                            }
                            if (CurrentSite == null) {
                                // read json file (based on AppSettings or default to SiteDefinition.json)
                                string siteFile = WebConfigHelper.GetValue<string>("YetaWF_Core", "SiteDefinition", "SiteDefinition.json")!;
                                string filePath = Path.Combine(YetaWFManager.RootFolder, siteFile);
                                if (File.Exists(filePath)) {
                                    string siteDefJson = File.ReadAllText(filePath); // use local file system as we need this during initialization
                                    CurrentSite = Utility.JsonDeserialize<SiteDefinition>(siteDefJson);
                                }
                            }
                            if (CurrentSite == null) {
                                // fallback to default identity
                                CurrentSite = new SiteDefinition {
                                    Identity = SiteDefinition.SiteIdentitySeed
                                };
                            }

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
    }
}
