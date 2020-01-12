/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

#if MVC6

using FluffySpoon.AspNet.LetsEncrypt.Certes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;
using YetaWF.Core.Support;
using YetaWF2.Logger;

namespace YetaWF.Core.WebStartup {

    public partial class StartupMVC6 {

        /// <summary>
        /// The main application entry point.
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        public static void Main(string[] args) {

            string currPath = Directory.GetCurrentDirectory();

            if (Startup.RunningInContainer) {
                string dataFolder = Path.Combine(currPath, Globals.DataFolder);
                if (!Directory.Exists(dataFolder) || IsEmptyDirectory(dataFolder)) {
                    System.Console.WriteLine($"Initializing {dataFolder}");
                    // If we don't have a Data folder, copy the /DataInit folder to /Data
                    // This is needed with Docker during first-time installs.
                    string dataInitFolder = Path.Combine(currPath, "DataInit");
                    CopyFiles(dataInitFolder, dataFolder);
                }
                string dataLocalFolder = Path.Combine(currPath, Globals.DataLocalFolder);
                if (!Directory.Exists(dataLocalFolder) || IsEmptyDirectory(dataLocalFolder)) {
                    System.Console.WriteLine($"Initializing {dataLocalFolder}");
                    // If we don't have a DataLocal folder, copy the /DataLocalInit folder to /DataLocal
                    // This is needed with Docker during first-time installs.
                    string dataLocalInitFolder = Path.Combine(currPath, "DataLocalInit");
                    CopyFiles(dataLocalInitFolder, dataLocalFolder);
                }
                string maintFolder = Path.Combine(currPath, "wwwroot", "Maintenance");
                if (!Directory.Exists(maintFolder) || IsEmptyDirectory(maintFolder)) {
                    System.Console.WriteLine($"Initializing {maintFolder}");
                    // If we don't have a Maintenance folder, copy the MaintenanceInit folder to Maintenance
                    // This is needed with Docker during first-time installs.
                    string maintInitFolder = Path.Combine(currPath, "wwwroot", "MaintenanceInit");
                    CopyFiles(maintInitFolder, maintFolder);
                }
            }

            string hosting = GetHostingFile();

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
                        kestrelOptions.ConfigureHttpsDefaults(httpsOptions => httpsOptions.ServerCertificateSelector = (c, s) => LetsEncryptRenewalService.Certificate);
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

            host.Run();
        }

        /// <summary>
        /// Returns an environment and runtime specific AppSettings.json file name.
        /// </summary>
        /// <returns>Returns an environment and runtime specific AppSettings.json file name.</returns>
        public static string GetAppSettingsFile() {
            if (_AppSettingsFile == null)
                _AppSettingsFile = Startup.GetEnvironmentFile(Path.Combine(Directory.GetCurrentDirectory(), Globals.DataFolder), "AppSettings", "json");
            return _AppSettingsFile;
        }
        private static string _AppSettingsFile = null;

        /// <summary>
        /// Returns an environment and runtime specific hosting.json file name.
        /// </summary>
        /// <returns>Returns an environment and runtime specific hosting.json file name.</returns>
        public static string GetHostingFile() {
            if (_HostingFile == null)
                _HostingFile = Startup.GetEnvironmentFile(Directory.GetCurrentDirectory(), "hosting", "json", Optional: true);
            return _HostingFile;
        }
        private static string _HostingFile = null;

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
        private static bool IsEmptyDirectory(string folder) {
            return Directory.GetFiles(folder).Length == 0 && Directory.GetDirectories(folder).Length == 0;
        }
    }

}

#endif