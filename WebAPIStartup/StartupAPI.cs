/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using YetaWF.Core.Language;
using YetaWF.Core.Support;

namespace YetaWF.Core.WebAPIStartup {

    /// <summary>
    /// The class implenting a YetaWF API service .
    /// </summary>
    public class StartupAPI {

        /// <summary>
        /// The main entry point of a YetaWF API service.
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        public static void Main(string[] args) {

            string currPath = AppDomain.CurrentDomain.BaseDirectory;
            YetaWFManager.RootFolder = currPath;
            YetaWFManager.RootFolderWebProject = currPath;

            string appSettings = YetaWF.Core.Support.Startup.GetEnvironmentFile(currPath, "AppSettings", "json");

            WebConfigHelper.InitAsync(appSettings).Wait();
            LanguageSection.InitAsync(Path.Combine(currPath, YetaWF.Core.Support.Startup.LANGUAGESETTINGS)).Wait();

            IConfigurationRoot config = new ConfigurationBuilder()
                .SetBasePath(currPath)
                .AddJsonFile(appSettings)
                .AddEnvironmentVariables()
                .Build();

            string urls = WebConfigHelper.GetValue<string>("Default", "URL");
            if (urls == null)
                throw new InternalError("No URL defined");

            IHost host = new HostBuilder()
                .UseContentRoot(currPath)
                .ConfigureHostConfiguration(configHost => {
                    configHost.SetBasePath(currPath);
                    configHost.AddJsonFile(appSettings, false, reloadOnChange: false); // needed for logging
                    //configHost.AddEnvironmentVariables(prefix: "");
                    configHost.AddCommandLine(args);
                })
                .ConfigureWebHostDefaults(webBuilder => {
                    webBuilder.UseUrls(urls);
                    webBuilder.UseKestrel();
                    webBuilder.UseIIS();
                    webBuilder.UseIISIntegration();
                    webBuilder.CaptureStartupErrors(true);
                    webBuilder.UseSetting(WebHostDefaults.DetailedErrorsKey, "true");
                    webBuilder.UseStartup<Startup>();
                })
                .ConfigureAppConfiguration((bldr) => {
                    bldr.AddEnvironmentVariables();
                })
                .ConfigureLogging((hostingContext, logging) => {
                    logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                    logging.AddConsole();
                    logging.AddEventSourceLogger();
                    //logging.AddYetaWFLogger(); // we don't use this because services log to the MS log
#if DEBUG
                    logging.AddDebug();
#endif
                })
                .Build();

            host.Run();
        }
    }
}
