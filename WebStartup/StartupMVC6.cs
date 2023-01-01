/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using YetaWF.Core.Support;
using YetaWF2.Logger;

namespace YetaWF.Core.WebStartup {

    public partial class StartupMVC6 {

        /// <summary>
        /// The main application entry point.
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        /// <param name="preRun">An optional method to execute before starting the application.</param>
        public static void Main(string[] args, Action? preRun = null) {

            string currPath = Directory.GetCurrentDirectory();

            if (Startup.RunningInContainer) {
                {
                    // Copy any new files from /DataInit to /Data
                    // This is needed with Docker during first-time installs if a DataLocalInit folder is present.
                    string dataInitFolder = Path.Combine(currPath, "DataInit");
                    if (Directory.Exists(dataInitFolder)) { // this is optional
                        string dataFolder = Path.Combine(currPath, Globals.DataFolder);
                        System.Console.WriteLine($"Initializing {dataFolder}");
                        CopyMissingFiles(dataInitFolder, dataFolder);
                    }
                }
                {
                    // Copy any new files from /DataLocalInit to /DataLocal
                    // This is needed with Docker during first-time installs if a DataLocalInit folder is present.
                    string dataLocalInitFolder = Path.Combine(currPath, "DataLocalInit");
                    if (Directory.Exists(dataLocalInitFolder)) { // this is optional
                        string dataLocalFolder = Path.Combine(currPath, Globals.DataLocalFolder);
                        System.Console.WriteLine($"Initializing {dataLocalFolder}");
                        CopyMissingFiles(dataLocalInitFolder, dataLocalFolder);
                    }
                }
                {
                    // Copy any new files from the ./wwwroot/MaintenanceInit folder to ./wwwroot/Maintenance
                    // This is needed with Docker during first-time installs.
                    string maintInitFolder = Path.Combine(currPath, "wwwroot", "MaintenanceInit");
                    if (Directory.Exists(maintInitFolder)) { // this is optional
                        string maintFolder = Path.Combine(currPath, "wwwroot", "Maintenance");
                        System.Console.WriteLine($"Initializing {maintFolder}");
                        CopyMissingFiles(maintInitFolder, maintFolder);
                    }
                }
            }

            string? hosting = GetHostingFile();

            IHost host = new HostBuilder()
                .UseContentRoot(currPath)
                .ConfigureHostConfiguration(configHost => {
                    configHost.SetBasePath(currPath);
                    configHost.AddJsonFile(GetAppSettingsFile(), reloadOnChange: false, optional: false); // needed for logging
                    if (hosting != null)
                        configHost.AddJsonFile(hosting);
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
                    webBuilder.UseStartup<StartupMVC6>();
                })
                .ConfigureAppConfiguration((bldr) => {
                    bldr.AddEnvironmentVariables();
                })
                .ConfigureLogging((hostingContext, logging) => {
                    logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                    logging.AddConsole();
                    logging.AddEventSourceLogger();
                    logging.AddYetaWFLogger();
#if DEBUG
                    logging.AddDebug();
#endif
                })
                .Build();

            if (preRun != null)
                preRun();

            host.Run();
        }

        /// <summary>
        /// Returns an environment and runtime specific AppSettings.json file name.
        /// </summary>
        /// <returns>Returns an environment and runtime specific AppSettings.json file name.</returns>
        public static string GetAppSettingsFile() {
            if (_AppSettingsFile == null)
                _AppSettingsFile = Startup.GetEnvironmentFile(Path.Combine(Directory.GetCurrentDirectory(), Globals.DataFolder), "AppSettings", "json")!;
            return _AppSettingsFile;
        }
        private static string? _AppSettingsFile = null;

        /// <summary>
        /// Returns an environment and runtime specific hosting.json file name.
        /// </summary>
        /// <returns>Returns an environment and runtime specific hosting.json file name.</returns>
        public static string? GetHostingFile() {
            return Startup.GetEnvironmentFile(Directory.GetCurrentDirectory(), "hosting", "json", Optional: true);
        }

        //private static void CopyFiles(string srcInitFolder, string targetFolder) {
        //    Directory.CreateDirectory(targetFolder);
        //    string[] files = Directory.GetFiles(srcInitFolder);
        //    foreach (string file in files) {
        //        File.Copy(file, Path.Combine(targetFolder, Path.GetFileName(file)));
        //    }
        //    string[] dirs = Directory.GetDirectories(srcInitFolder);
        //    foreach (string dir in dirs) {
        //        CopyFiles(dir, Path.Combine(targetFolder, Path.GetFileName(dir)));
        //    }
        //}
        //private static bool IsEmptyDirectory(string folder) {
        //    return Directory.GetFiles(folder).Length == 0 && Directory.GetDirectories(folder).Length == 0;
        //}
        private static void CopyMissingFiles(string srcInitFolder, string targetFolder) {
            Directory.CreateDirectory(targetFolder);
            string[] files = Directory.GetFiles(srcInitFolder);
            foreach (string file in files) {
                string targetFile = Path.Combine(targetFolder, Path.GetFileName(file));
                if (!File.Exists(targetFile))
                    File.Copy(file, targetFile);
            }
            string[] dirs = Directory.GetDirectories(srcInitFolder);
            foreach (string dir in dirs) {
                CopyMissingFiles(dir, Path.Combine(targetFolder, Path.GetFileName(dir)));
            }
        }
    }

}
