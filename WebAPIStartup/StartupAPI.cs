/* Copyright © 2021 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

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
    /// The class implementing a YetaWF API service .
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

            // If there is no Data folder but we have a DataInit folder, copy contents to Data folder (first time install)
            string dataFolder = Path.Combine(currPath, Globals.DataFolder);
            if (!Directory.Exists(dataFolder) || IsEmptyDirectory(dataFolder)) {
                string initFolder = Path.Combine(currPath, "DataInit");
                if (Directory.Exists(initFolder)) {
                    System.Console.WriteLine($"Initializing {dataFolder}");
                    // If we don't have a Data folder, copy the /DataInit folder to /Data
                    // This is needed with Docker during first-time installs.
                    string dataInitFolder = Path.Combine(currPath, "DataInit");
                    CopyFiles(dataInitFolder, dataFolder);
                }
            }

            string appSettings = YetaWF.Core.Support.Startup.GetEnvironmentFile(currPath, "AppSettings", "json")!;

            WebConfigHelper.InitAsync(appSettings).Wait();
            LanguageSection.InitAsync(Path.Combine(currPath, YetaWF.Core.Support.Startup.LANGUAGESETTINGS)).Wait();

            IConfigurationRoot config = new ConfigurationBuilder()
                .SetBasePath(currPath)
                .AddJsonFile(appSettings)
                .AddEnvironmentVariables()
                .Build();

            IHost host = new HostBuilder()
                .UseContentRoot(currPath)
                .ConfigureHostConfiguration(configHost => {
                    configHost.SetBasePath(currPath);
                    configHost.AddJsonFile(appSettings, false, reloadOnChange: false); // needed for logging
                    //configHost.AddEnvironmentVariables(prefix: "");
                    configHost.AddCommandLine(args);
                })
                .ConfigureWebHostDefaults(webBuilder => {
                    webBuilder.UseKestrel(kestrelOptions => {
                        long? maxReq = WebConfigHelper.GetValue<long?>("YetaWF_Core", "MaxRequestBodySize", 30000000);
                        if (maxReq == 0)
                            kestrelOptions.Limits.MaxRequestBodySize = null;
                        else
                            kestrelOptions.Limits.MaxRequestBodySize = maxReq;
                    });
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
        private static bool IsEmptyDirectory(string folder) {
            return Directory.GetFiles(folder).Length == 0 && Directory.GetDirectories(folder).Length == 0;
        }
        private static void CopyFiles(string srcInitFolder, string targetFolder) {
            Directory.CreateDirectory(targetFolder);
            string[] files = Directory.GetFiles(srcInitFolder);
            foreach (string file in files) {
                File.Copy(file, Path.Combine(targetFolder, Path.GetFileName(file)));
            }
            string[] dirs = Directory.GetDirectories(srcInitFolder);
            foreach (string dir in dirs) {
                CopyFiles(dir, Path.Combine(targetFolder, Path.GetFileName(dir)));
            }
        }
    }
}
