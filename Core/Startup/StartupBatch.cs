/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using YetaWF.Core.Controllers;
using YetaWF.Core.DataProvider;
using YetaWF.Core.Language;
using YetaWF.Core.Site;

namespace YetaWF.Core.Support {

    /// <summary>
    /// This class is used by a console application during startup to initialize packages and perform startup processing.
    /// </summary>
    public static class StartupBatch {

        /// <summary>
        /// Called to initialize a console application so it can use all of YetaWF's services, including data providers, caching, etc.
        /// </summary>
        /// <param name="baseDirectory">The base folder where the executable and all assemblies for the console application are located.</param>
        /// <param name="siteDomain">The domain name used to access data. This must be an existing domain with a YetaWF site and AppSettings.json must contain data provider information.</param>
        /// <remarks>
        /// The Start method makes all settings from AppSettings.json available. In release builds, AppSettings.PROD.json is used instead.
        ///
        /// A LanguageSettings.json file must be present defining all languages use by the specified site <paramref name="siteDomain"/> (a copy of the LanguageSettings.json file used for the website).
        ///
        /// The console application must have references to the YetaWF.Core, YetaWF.Caching and YetaWF.SiteProperties packages.
        /// Additional references must be added for services used by the console application.
        ///
        /// Because all YetaWF services are available, all data providers and config settings can be accessed (and modified).
        /// Many data providers use site specific data. The data for the specified site <paramref name="siteDomain"/> is used.
        /// </remarks>
        public static void Start(string baseDirectory, string siteDomain) {

            YetaWFManager.RootFolder = baseDirectory;
#if MVC6
            YetaWFManager.RootFolderWebProject = baseDirectory;
#endif
            WebConfigHelper.InitAsync(Path.Combine(baseDirectory, YetaWF.Core.Support.Startup.APPSETTINGS)).Wait();
            LanguageSection.InitAsync(Path.Combine(baseDirectory, YetaWF.Core.Support.Startup.LANGUAGESETTINGS)).Wait();

            // Initialize
            YetaWFManager.MakeInitialThreadInstance(null);
#if MVC6
            YetaWFManager.Init();
#endif
            YetaWFManager.Syncify(async () => {

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

                // Set up specific site to use
                YetaWFManager.Manager.CurrentSite = await SiteDefinition.LoadSiteDefinitionAsync(siteDomain);

                YetaWF.Core.Support.Startup.Started = true;
            });
        }
    }
}
