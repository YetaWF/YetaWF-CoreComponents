/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using YetaWF.Core.Controllers;
using YetaWF.Core.DataProvider;
using YetaWF.Core.Language;
using YetaWF.Core.Site;
using YetaWF.Core.Support;

namespace YetaWF.Core.ConsoleStartup {

    /// <summary>
    /// This class is used by a console application during startup to initialize packages and perform startup processing.
    /// </summary>
    public static class StartupConsole {

        /// <summary>
        /// Called to initialize a console application so it can use all of YetaWF's services, including data providers, caching, etc.
        /// </summary>
        /// <param name="baseDirectory">The base folder where the executable and all assemblies for the console application are located.</param>
        /// <param name="siteDomain">The domain name used to access data. This must be an existing domain with a YetaWF site and AppSettings.json must contain data provider information.
        /// May be null to load use the default site.
        /// </param>
        /// <remarks>
        /// The Start method makes all settings from AppSettings.json available.
        ///
        /// A LanguageSettings.json file must be present defining all languages used by the specified site <paramref name="siteDomain"/> (a copy of the LanguageSettings.json file used for the website).
        ///
        /// The console application must have references to the YetaWF.Core, YetaWF.Caching and YetaWF.SiteProperties packages.
        /// Additional references must be added for services used by the console application.
        ///
        /// Because all YetaWF services are available, all data providers and config settings can be accessed (and modified).
        /// Many data providers use site specific data. The data for the specified site <paramref name="siteDomain"/> is used.
        /// </remarks>
        public static void Start(string baseDirectory, string siteDomain) {

            Start(baseDirectory);

            YetaWFManager.Syncify((Func<System.Threading.Tasks.Task>)(async () => {
                // Set up specific site to use, requires YetaWF.SitePropertiesService
                SiteDefinition site = await SiteDefinition.LoadSiteDefinitionAsync(siteDomain);
                if (site == null)
                    throw new InternalError("Default site not defined (AppSettings.json) or not found");
                YetaWFManager.Manager.CurrentSite = site;

                YetaWF.Core.Support.Startup.Started = true;
            }));
        }
        /// <summary>
        /// Called to initialize a console application so it can use all of YetaWF's services, including data providers, caching, etc.
        /// </summary>
        /// <param name="baseDirectory">The base folder where the executable and all assemblies for the console application are located.</param>
        /// <param name="siteIdentity">The domain's site identity used to access data. This must be an existing domain with a YetaWF site and AppSettings.json must contain data provider information.</param>
        /// <remarks>
        /// The Start method makes all settings from AppSettings.json available.
        ///
        /// A LanguageSettings.json file must be present defining all languages used by the specified site <paramref name="siteIdentity"/> (a copy of the LanguageSettings.json file used for the website).
        ///
        /// The console application must have references to the YetaWF.Core and YetaWF.Caching. The YetaWF.SiteProperties package is not required.
        /// Using this method to start, limited services can be used. Any services that require a complete SiteDefinition (derived from the site's domain name) will fail.
        /// Additional references must be added for services used by the console application.
        ///
        /// Because all YetaWF services are available, all data providers and config settings can be accessed (and modified).
        /// Many data providers use site specific data. The data for the specified site <paramref name="siteIdentity"/> is used.
        /// </remarks>
        public static void StartByIdentity(string baseDirectory, int siteIdentity) {

            Start(baseDirectory);

            // Set up specific site to use
            YetaWFManager.Manager.CurrentSite = new SiteDefinition() {
                Identity = siteIdentity
            };

            YetaWF.Core.Support.Startup.Started = true;
        }

        /// <summary>
        /// Called to initialize a console application so it can use all of YetaWF's services, including data providers, caching, etc.
        /// </summary>
        /// <param name="baseDirectory">The base folder where the executable and all assemblies for the console application are located.</param>
        /// <param name="filePath">The path and file name of a file containing json describing the site to use. Files with json information are saved in the website's
        ///  ./Website/Data/Sites folder whenever site settings are saved.</param>
        /// <remarks>
        /// The Start method makes all settings from AppSettings.json available.
        ///
        /// A LanguageSettings.json file must be present defining all languages used by the specified site <paramref name="filePath"/> (a copy of the LanguageSettings.json file used for the website).
        ///
        /// The console application must have references to the YetaWF.Core and YetaWF.Caching. The YetaWF.SiteProperties package is not required.
        /// Using this method to start, limited services can be used.
        /// Additional references must be added for services used by the console application.
        ///
        /// Because all YetaWF services are available, all data providers and config settings can be accessed (and modified).
        /// Many data providers use site specific data. The data for the specified site <paramref name="filePath"/> is used.
        /// </remarks>
        public static void StartBySiteDefinitionFile(string baseDirectory, string filePath) {

            Start(baseDirectory);

            // Set up specific site to use
            string siteDefJson = File.ReadAllText(filePath); // use local file system as we need this during initialization
            SiteDefinition site = Utility.JsonDeserialize<SiteDefinition>(siteDefJson);
            YetaWFManager.Manager.CurrentSite = site;

            YetaWF.Core.Support.Startup.Started = true;
        }

        private static void Start(string baseDirectory) {

            YetaWFManager.Mode = YetaWFManager.BATCHMODE;

            // Enable all required protocols
#if MVC6
            ServicePointManager.SecurityProtocol = ServicePointManager.SecurityProtocol | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
#else
            ServicePointManager.SecurityProtocol = ServicePointManager.SecurityProtocol | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
#endif

            YetaWFManager.RootFolder = baseDirectory;
#if MVC6
            YetaWFManager.RootFolderWebProject = baseDirectory;
#endif
            WebConfigHelper.InitAsync(YetaWF.Core.Support.Startup.GetEnvironmentFile(baseDirectory, "AppSettings", "json")).Wait();
            LanguageSection.InitAsync(Path.Combine(baseDirectory, YetaWF.Core.Support.Startup.LANGUAGESETTINGS)).Wait();

            // Initialize
            YetaWFManager.MakeInitialThreadInstance(null);
#if MVC6
            YetaWFManager.Init();
#endif
            YetaWFManager.Syncify((Func<System.Threading.Tasks.Task>)(async () => {

                // Set up areas (load all dlls/packages explicitly)
                List<string> files = Directory.GetFiles(baseDirectory, "*.dll").ToList();
                foreach (string file in files) {
                    try {
                        Assembly.LoadFrom(file);
                    } catch (Exception) { }
                }

                // Register all areas
                AreaRegistrationBase.RegisterPackages();
                // Register external data providers
                ExternalDataProviders.RegisterExternalDataProviders();

                // Call all classes that expose the interface IInitializeApplicationStartup
                YetaWFManager.Manager.CurrentSite = new SiteDefinition();
                await YetaWF.Core.Support.Startup.CallStartupClassesAsync();

            }));
        }
    }
}
