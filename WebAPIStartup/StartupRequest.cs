/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Threading.Tasks;
using YetaWF.Core.Site;
using YetaWF.Core.DataProvider;
using YetaWF.Core.Log;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using YetaWF.Core.Support;
using System.IO;

namespace YetaWF.Core.WebAPIStartup {

    /// <summary>
    /// The class implementing YetaWF API service requests.
    /// </summary>
    public static class StartupRequest {

        static SiteDefinition CurrentSite = null;

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

            YetaWFManager manager = YetaWFManager.MakeInstance(httpContext, CurrentSite.SiteDomain);

            // Site properties are ONLY valid AFTER this call to YetaWFManager.MakeInstance

            manager.CurrentSite = CurrentSite;
            manager.IsStaticSite = false;
            manager.IsTestSite = false;
            manager.IsLocalHost = uri.IsLoopback;

            // Handle any headers that alter the requested url
            string hostUsed, portUsed, schemeUsed;

            hostUsed = (string)httpContext.Request.Headers["X-Forwarded-Host"] ?? (string)httpContext.Request.Headers["X-Original-Host"];
            portUsed = (string)httpContext.Request.Headers["X-Forwarded-Port"] ?? (string)httpContext.Request.Headers["X-Original-Port"];
            schemeUsed = (string)httpContext.Request.Headers["X-Forwarded-Proto"] ?? (string)httpContext.Request.Headers["X-Original-Proto"];

            manager.HostUsed = hostUsed ?? uri.Host;
            manager.HostPortUsed = uri.Port;
            if (!string.IsNullOrWhiteSpace(portUsed)) {
                try { manager.HostPortUsed = Convert.ToInt32(portUsed); } catch (Exception) { }
            }
            manager.HostSchemeUsed = schemeUsed ?? uri.Scheme;

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
        public static void StartYetaWF() {

            if (!YetaWF.Core.Support.Startup.Started) {

                lock (_lockObject) { // protect from duplicate startup

                    if (!YetaWF.Core.Support.Startup.Started) {

                        YetaWFManager.Syncify(async () => { // startup code

                            YetaWFManager.Mode = YetaWFManager.SERVICEMODE;

                            // Create a startup log file
                            StartupLogging startupLog = new StartupLogging();
                            await Logging.RegisterLoggingAsync(startupLog);

                            Logging.AddLog($"{nameof(StartYetaWF)} starting");

                            YetaWFManager manager = YetaWFManager.MakeInitialThreadInstance(new SiteDefinition() { SiteDomain = YetaWFManager.SERVICEMODE }, null); // while loading packages we need a manager
                            YetaWFManager.Syncify(async () => {
                                // External data providers
                                ExternalDataProviders.RegisterExternalDataProviders();
                                // Call all classes that expose the interface IInitializeApplicationStartup
                                await YetaWF.Core.Support.Startup.CallStartupClassesAsync();

                                // Get default site
                                CurrentSite = SiteDefinition.LoadSiteDefinitionAsync != null ? await SiteDefinition.LoadSiteDefinitionAsync(null) : null;// Requires YetaWF.SitePropertiesService if used
                                if (CurrentSite == null) {
                                    // read json file in ./Data/Sites
                                    string filePath = Path.Combine(YetaWFManager.RootFolder, "SiteDefinition.json");
                                    string siteDefJson = File.ReadAllText(filePath); // use local file system as we need this during initialization
                                    CurrentSite = Utility.JsonDeserialize<SiteDefinition>(siteDefJson);
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
}
