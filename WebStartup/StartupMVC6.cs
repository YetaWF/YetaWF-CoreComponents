/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

#if MVC6

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;
using YetaWF.Core;
using YetaWF.Core.Support;
using YetaWF2.Logger;

namespace YetaWF.WebStartup {

    public partial class StartupMVC6 {

        public static void Main(string[] args) {

            string currPath = Directory.GetCurrentDirectory();

            if (Startup.RunningInContainer) {
                string dataFolder = Path.Combine(currPath, Globals.DataFolder);
                if (!Directory.Exists(dataFolder)) {
                    // If we don't have a Data folder, copy the /DataInit folder to /Data
                    // This is needed with Docker during first-time installs.
                    string dataInitFolder = Path.Combine(currPath, "DataInit");
                    CopyFiles(dataInitFolder, dataFolder);
                }
                string maintFolder = Path.Combine(currPath, "wwwroot", "Maintenance");
                if (!Directory.Exists(maintFolder)) {
                    // If we don't have a Maintenance folder, copy the MaintenanceInit folder to Maintenance
                    // This is needed with Docker during first-time installs.
                    string maintInitFolder = Path.Combine(currPath, "wwwroot", "MaintenanceInit");
                    CopyFiles(maintInitFolder, maintFolder);
                }
            }

            string hosting = GetHostingFile();

            //Host.CreateDefaultBuilder(args)

            new HostBuilder()
                .UseContentRoot(currPath)
                .ConfigureHostConfiguration(configHost => {
                    configHost.SetBasePath(currPath);
                    configHost.AddJsonFile(GetAppSettingsFile(), false, reloadOnChange: false); // needed for logging
                    configHost.AddJsonFile(hosting, optional: true);
                    //configHost.AddEnvironmentVariables(prefix: "");
                    configHost.AddCommandLine(args);
                })
                .ConfigureWebHostDefaults(webBuilder => {
                    webBuilder.UseKestrel();
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
                .Start();
        }

        public static string GetAppSettingsFile() {
            if (_AppSettingsFile == null)
                _AppSettingsFile = Startup.GetEnvironmentFile(Path.Combine(Directory.GetCurrentDirectory(), Globals.DataFolder), "AppSettings", "json");
            return _AppSettingsFile;
        }
        private static string _AppSettingsFile = null;

        public static string GetHostingFile() {
            if (_HostingFile == null)
                _HostingFile = Startup.GetEnvironmentFile(Directory.GetCurrentDirectory(), "hosting", "json");
            return _HostingFile;
        }
        private static string _HostingFile = null;

        private static void CopyFiles(string srcInitFolder, string srcFolder) {
            Directory.CreateDirectory(srcFolder);
            string[] files = Directory.GetFiles(srcInitFolder);
            foreach (string file in files) {
                File.Copy(file, Path.Combine(srcFolder, Path.GetFileName(file)));
            }
            string[] dirs = Directory.GetDirectories(srcInitFolder);
            foreach (string dir in dirs) {
                CopyFiles(dir, Path.Combine(srcFolder, Path.GetFileName(dir)));
            }
        }
    }

}

#endif