﻿/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetaWF.Core.IO;
using YetaWF.Core.Log;
using YetaWF.Core.Pages;
using YetaWF.Core.Site;
using YetaWF.Core.Support;

namespace YetaWF.Core.Packages {

    public partial class Package {

        /* private static string __ResStr(string name, string defaultValue, params object?[] parms) { return ResourceAccess.GetResourceString(typeof(Package), name, defaultValue, parms); } */

        private class PackageInfo {
            public string Name { get; set; } = null!;
            public string Version { get; set; } = null!;
        }

        /// <summary>
        /// Saves a map of all currently installed packages.
        /// </summary>
        /// <remarks>
        /// The package map is automatically recreated every time the site is restarted.
        ///
        /// The package map is saved at .\Website\Data\PackageMap.txt
        /// </remarks>
        public static async Task SavePackageMapAsync() {
            Logging.AddLog("Saving package map");
            string rootFolder = YetaWFManager.RootFolderWebProject;
            string outFile = Path.Combine(rootFolder, Globals.DataFolder, Globals.PackageMap);
            StringBuilder sb = new StringBuilder();
            List<Package> packages = GetAvailablePackages();
            packages = (from p in packages orderby p.Name select p).ToList();
            foreach (Package package in packages) {
                sb.Append(string.Format("{0} {1}\r\n", package.Name, package.Version));
            }
            await FileSystem.FileSystemProvider.WriteAllTextAsync(outFile, sb.ToString());
            Logging.AddLog("Saving package map completed");
        }

        /// <summary>
        /// Takes the existing package map (from a prior YetaWF instance startup) and installs or updates models
        /// for new packages and packages whose version has been updated.
        /// </summary>
        /// <remarks>
        /// The saved package map (from a prior YetaWF instance startup) is used during YetaWF startup to install or update models
        /// for new packages and packages whose version has been updated.
        ///
        /// For example, if package YetaWF.Text in the package map specifies version 1.0.1, but during the next YetaWF restart, version 1.0.2 is detected,
        /// models for the YetaWF.Text package are updated and the Site Template YetaWF_Text.1.0.2.txt is executed
        /// (dots (.) are replaced by underscores (_) in package names, followed by .{i}version{/i}.txt to determine the Site Template name).
        ///
        /// A log file recording all upgrade activity is saved at .\Website\Data\UpgradeLogFile.txt
        /// </remarks>
        public static async Task UpgradeToNewPackagesAsync() {

            if (SiteDefinition.INITIAL_INSTALL) return;
            if (YetaWFManager.Deployed && !await MustUpgradeAsync()) return;

            //File.Delete(Path.Combine(YetaWFManager.RootFolder, Globals.DataFolder, Globals.UpgradeLogFile));

            // Create an update log file
            UpgradeLogging log = new UpgradeLogging();
            await Logging.RegisterLoggingAsync(log);

            SiteDefinition? site = await SiteDefinition.LoadSiteDefinitionAsync(null);// get the default site
            if (site == null)
                throw new InternalError("Couldn't obtain default site");
            SiteDefinition origSite = YetaWFManager.Manager.CurrentSite;
            YetaWFManager.Manager.CurrentSite = site;

            try {
                await UpgradePackagesAsync();
            } catch (Exception) {
                throw;
            } finally {
                // Stop update log file
                Logging.UnregisterLogging(log);
                YetaWFManager.Manager.CurrentSite = origSite;
            }
        }

        /// <summary>
        /// Implements a logging data provider to record all logging data while upgrading a site.
        /// </summary>
        /// <remarks>
        /// All data recorded while upgrading a site is written to the file .\Website\Data\UpgradeLogFile.txt.
        /// </remarks>
        public class UpgradeLogging : ILogging {

            private string LogFile { get; set; }

            /// <summary>
            /// Constructor.
            /// </summary>
            public UpgradeLogging() {
                string rootFolder = YetaWFManager.RootFolderWebProject;
                LogFile = Path.Combine(rootFolder, Globals.DataFolder, Globals.UpgradeLogFile);
            }
            /// <summary>
            /// Returns the minimum severity level that is logged by the logging data provider.
            /// </summary>
            /// <returns>Returns the minimum severity level that is logged by the logging data provider.</returns>
            public Logging.LevelEnum GetLevel() { return Logging.LevelEnum.Info; }
            /// <summary>
            /// Initializes the logging data provider.
            /// </summary>
            public Task InitAsync() { return Task.CompletedTask; }
            /// <summary>
            /// Clears all log records.
            /// </summary>
            public Task ClearAsync() { return Task.CompletedTask; }
            /// <summary>
            /// Flushes all pending log records to permanent storage.
            /// </summary>
            public Task FlushAsync() { return Task.CompletedTask; }
            /// <summary>
            /// Returns whether the logging data provider is installed and available.
            /// </summary>
            /// <returns>Returns whether the logging data provider is installed and available.</returns>
            public Task<bool> IsInstalledAsync() { return Task.FromResult(true); }
            /// <summary>
            /// Adds a record using the logging data provider.
            /// </summary>
            /// <param name="category">The event name or category.</param>
            /// <param name="level">The severity level of the message.</param>
            /// <param name="relStack">The number of call stack entries that should be skipped when generating a call stack.</param>
            /// <param name="text">The log message.</param>
            public void WriteToLogFile(string category, Logging.LevelEnum level, int relStack, string text) {
                FileSystem.FileSystemProvider.AppendAllTextAsync(LogFile, $"{DateTime.Now} {text}\r\n").Wait();// uhm yeah, only while upgrading
            }
            /// <summary>
            /// Defines whether the logging data provider is already logging an event.
            /// </summary>
            bool ILogging.IsProcessing { get; set; }
        }
        /// <summary>
        /// Returns whether an upgrade is forced (even on deployed systems).
        /// </summary>
        /// <returns>Returns true if an upgrade is forced, false otherwise.</returns>
        /// <remarks>
        /// During application startup, an upgrade of all installable models can be forced by placing an empty file named UpdateIndicator.txt into the root folder of the website.
        /// </remarks>
        public static async Task<bool> MustUpgradeAsync() {
            return await FileSystem.FileSystemProvider.FileExistsAsync(GetUpdateIndicatorFileName());
        }
        private static string GetUpdateIndicatorFileName() {
            string rootFolder = YetaWFManager.RootFolderWebProject;
            return Path.Combine(rootFolder, Globals.UpdateIndicatorFile);
        }

        private static async Task UpgradePackagesAsync() {

            Logging.AddLog("Upgrading to new packages");

            List<PackageInfo> list = await LoadPackageMapAsync();

            // get all currently installed packages
            List<Package> allPackages = Package.GetAvailablePackages();

            // update/create all models
            if (await MustUpgradeAsync()) {
                Logging.AddLog("Updating all packages");
                await UpdateAllAsync();
                Logging.AddLog("Updating models for all packages completed");
            } else {
                // update models
                Logging.AddLog("Updating models for changed packages");
                foreach (Package package in allPackages) {
                    PackageInfo? info = (from p in list where p.Name == package.Name select p).FirstOrDefault();
                    string? lastSeenVersion;
                    if (info == null) {
                        // brand new package
                        Logging.AddLog("New package {0}", package.Name);
                        lastSeenVersion = null;
                    } else {
                        lastSeenVersion = info.Version;
                    }
                    int cmp = Package.CompareVersion(lastSeenVersion, package.Version);
                    if (cmp < 0) {
                        // upgraded package
                        if (string.IsNullOrWhiteSpace(lastSeenVersion)) {
                            Logging.AddLog("Installing package {0} {1}", package.Name, package.Version);
                            await InstallPackageAsync(package);
                        } else {
                            Logging.AddLog("Upgrading package {0} from {1} to {2}", package.Name, lastSeenVersion, package.Version);
                            await InstallPackageAsync(package, lastSeenVersion);
                        }
                    } else if (cmp > 0) {
                        // Woah, you can't downgrade
                        throw new InternalError("Found package {0},{1} which is a lower version than the previous version {2}", package.Name, package.Version, lastSeenVersion);
                    } else { /* same version */

                    }
                }
                Logging.AddLog("Updating models for changed packages completed");
            }

            // Run Site Templates for new/updated packages
            foreach (Package package in allPackages) {
                PackageInfo? info = (from p in list where p.Name == package.Name select p).FirstOrDefault();
                if (info == null) {
                    // new package
                    await InstallSiteTemplateAsync(package, string.Empty);
                    await ImportPagesAsync(package, string.Empty);
                } else {
                    int cmp = Package.CompareVersion(info.Version, package.Version);
                    if (cmp < 0) {
                        // upgraded package
                        await InstallSiteTemplateAsync(package, info.Version);
                        await ImportPagesAsync(package, info.Version);
                    } else if (cmp > 0) {
                        // Woah, you can't downgrade
                        throw new InternalError("Found package {0},{1} which is a lower version than the previous version {2}", package.Name, package.Version, info.Version);
                    } else { /* same version */

                    }
                }
            }

            // Remove the update indicator file (if present)
            await FileSystem.FileSystemProvider.DeleteFileAsync(GetUpdateIndicatorFileName());

            // Save new package map (only if successful)
            await Package.SavePackageMapAsync();

            // Scan website's Addons folder for packages that aren't referenced and remove folder

            Logging.AddLog("Removing unnecessary website Addons folders");

            List<Package> packages = GetAvailablePackages();
            List<string> usedFolders = (from p in packages select p.AddonsSourceFolder).ToList();
            List<string> domains = await FileSystem.FileSystemProvider.GetDirectoriesAsync(Path.Combine(YetaWFManager.RootFolder, Globals.AddOnsFolder));
            foreach (string domain in domains) {
                List<string> definedFolders = await FileSystem.FileSystemProvider.GetDirectoriesAsync(domain);
                foreach (string definedFolder in definedFolders) {
                    if (!usedFolders.Contains(definedFolder)) {
                        await FileSystem.FileSystemProvider.DeleteDirectoryAsync(definedFolder);
                    }
                }
            }
        }

        /// <summary>
        /// Loads the existing package map at .\Website\Data\PackageMap.txt
        /// </summary>
        /// <returns>Information for all packages that were available during the last startup of YetaWF.</returns>
        private static async Task<List<PackageInfo>> LoadPackageMapAsync() {
            List<PackageInfo> list = new List<PackageInfo>();
            string rootFolder = YetaWFManager.RootFolderWebProject;
            string inFile = Path.Combine(rootFolder, Globals.DataFolder, Globals.PackageMap);
            if (!await FileSystem.FileSystemProvider.FileExistsAsync(inFile))
                throw new InternalError("The package map file {0} does not exist", inFile);
            List<string> lines = await FileSystem.FileSystemProvider.ReadAllLinesAsync(inFile);
            int count = 1;
            foreach (string line in lines) {
                if (!string.IsNullOrWhiteSpace(line)) {
                    string[] parts = line.Split(new char[] { ' ' }, 2);
                    if (parts.Length != 2)
                        throw new InternalError("Package map data {0}(line {1} is invalid - see {2}", line, count, inFile);
                    PackageInfo package = new PackageInfo {
                        Name = parts[0],
                        Version = parts[1],
                    };
                    list.Add(package);
                }
                count++;
            }
            return list;
        }

        /// <summary>
        /// Updates all models for all packages.
        /// </summary>
        private static async Task UpdateAllAsync() {
            // create models for each package
            // order all available packages by service level so we start them up in the correct order
            List<Package> packages = (from p in Package.GetAvailablePackages() orderby (int)p.ServiceLevel select p).ToList();
            foreach (Package package in packages) {
                await InstallPackageAsync(package);
            }
        }
        /// <summary>
        /// Creates/updates models for one package
        /// </summary>
        /// <param name="package"></param>
        private static async Task InstallPackageAsync(Package package, string? lastSeenVersion = null) {
            Logging.AddLog("Creating/updating {0}", package.Name);
            List<string> errorList = new List<string>();
            if (!await package.InstallModelsAsync(errorList, lastSeenVersion)) {
                ScriptBuilder sb = new ScriptBuilder();
                sb.Append(__ResStr("cantInstallModels", "Can't install models for package {0}:(+nl)"), package.Name);
                sb.Append(errorList, LeadingNL: true);
                Logging.AddErrorLog("Failure in {0}", package.Name, errorList);
                throw new InternalError(sb.ToString());
            }
        }

        /// <summary>
        /// Imports pages for the specified package to upgrade it to the new package version.
        /// </summary>
        /// <param name="package">The package for which pages need to be imported.</param>
        /// <param name="lastSeenVersion">The last package version that was installed. null if this is a new package.</param>
        /// <remarks>
        /// For new packages, the zip files in the Install folder are imported first.
        /// All page zip files are imported between the last seen version and the current version to bring the package to its new version.
        /// </remarks>
        private static async Task ImportPagesAsync(Package package, string? lastSeenVersion) {
            string rootFolder = YetaWFManager.RootFolderWebProject;
            string packageFolder = Path.Combine(rootFolder, Globals.SiteTemplates, package.AreaName);
            if (await FileSystem.FileSystemProvider.DirectoryExistsAsync(packageFolder)) {
                List<string> versionFolders = await FileSystem.FileSystemProvider.GetDirectoriesAsync(packageFolder);
                if (lastSeenVersion == null) {
                    string? installFolder = (from v in versionFolders where v.EndsWith("Install") select v).FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(installFolder)) {
                        await ImportPagesAsync(installFolder);
                    }
                    lastSeenVersion = "0.0.0";
                }
                versionFolders = (from v in versionFolders where !v.EndsWith("Install") select v).ToList();
                versionFolders.Sort(new PackageFolderComparer());
                // directories are now sorted by version, process in this order (oldest to newest)
                foreach (string versionFolder in versionFolders) {
                    string version = Path.GetFileName(versionFolder);
                    if (Package.CompareVersion(lastSeenVersion, version) < 0) {
                        await ImportPagesAsync(versionFolder);
                    }
                }
            }
        }

        private static async Task ImportPagesAsync(string versionFolder) {
            List<string> pageFiles = await FileSystem.FileSystemProvider.GetFilesAsync(versionFolder);
            foreach (string pageFile in pageFiles) {
                List<string> errorList = new List<string>();
                // import the page definition
                Logging.AddLog($"Importing {pageFile}");
                PageDefinition.ImportInfo info = await PageDefinition.ImportAsync(pageFile, errorList);
                if (errorList.Count > 0) {
                    Logging.AddErrorLog("An error occurred importing {0}", pageFile);
                    foreach (string error in errorList)
                        Logging.AddErrorLog(error);
                }
            }
        }


        /// <summary>
        /// Runs Site Templates for the specified package to upgrade it to the new package version.
        /// </summary>
        /// <param name="package">The package for which Site Templates need to be run.</param>
        /// <param name="lastSeenVersion">The last package version that was installed. null if this is a new package.</param>
        /// <remarks>
        /// All Site Templates are executed to bring the package to its new version.
        ///
        /// For new packages all templates for the package are run. For packages that where previously seen (with an older version),
        /// all templates with a newer version are executed.
        /// </remarks>
        private static async Task InstallSiteTemplateAsync(Package package, string? lastSeenVersion) {
            string rootFolder = YetaWFManager.RootFolderWebProject;
            string templateBase = package.Name.Replace(".", "_");
            string templateFolder = Path.Combine(rootFolder, Globals.SiteTemplates);
            await FileSystem.FileSystemProvider.CreateDirectoryAsync(templateFolder);
            List<string> templates = await FileSystem.FileSystemProvider.GetFilesAsync(templateFolder, templateBase + "*.txt");
            templates = (from t in templates select Path.GetFileNameWithoutExtension(t)).ToList();
            templates.Sort(new SiteTemplateNameComparer());
            // templates are now sorted by version, process in this order (oldest to newest)
            foreach (string template in templates) {
                string name, version;
                SiteTemplateNameComparer.GetComponents(template, out name, out version);
                bool installTemplate = false;
                if (string.IsNullOrWhiteSpace(lastSeenVersion)) {
                    // new package - only run the base Site Template
                    if (string.IsNullOrWhiteSpace(version))
                        installTemplate = true;
                } else {
                    if (!string.IsNullOrWhiteSpace(version) && Package.CompareVersion(lastSeenVersion, version) < 0) {
                        if (Package.CompareVersion(package.Version, version) >= 0) // make sure not to install templates that are newer than the package
                            installTemplate = true;
                    }
                }
                if (installTemplate) {
                    // execute this template via built-in command (implemented by the YetaWF.Package package)
                    Logging.AddLog("Executing site template {0}", template);
                    Func<QueryHelper, Task>? action = await BuiltinCommands.FindAsync("/$processtemplate", checkAuthorization: false);
                    if (action == null)
                        throw new InternalError("Built-in command /$processtemplate not found");
                    QueryHelper qs = new QueryHelper();
                    qs["Template"] = template + ".txt";
                    try {
                        await action(qs);
                    } catch (Exception exc) {
                        // errors in site templates are logged but will not end the upgrade process
                        Logging.AddErrorLog("An error occurred executing site template {0}", template, exc);
                    }
                }
            }
        }
        // used to compare site template names including version numbers
        private class SiteTemplateNameComparer : IComparer<string> {
            public int Compare(string? x, string? y) {
                if (x == null && y == null) return 0;
                if (x == null) return -1;
                if (y == null) return 1;
                // each site template is in the following form:
                // package_name.txt  or
                // package_name.version.txt where version is x.y.z
                // so we need to parse the file name to extract the version and compare name/version accordingly
                SiteTemplateNameComparer.GetComponents(x, out string xName, out string xVersion);
                string yName, yVersion;
                SiteTemplateNameComparer.GetComponents(y, out yName, out yVersion);
                int iName = string.Compare(xName, yName);
                if (iName != 0) return iName;// the names aren't the same
                // same name, so check version
                return Package.CompareVersion(xVersion, yVersion);
            }
            public static void GetComponents(string templateName, out string name, out string version) {
                int ix = templateName.IndexOf(".", StringComparison.Ordinal);
                if (ix < 0) { // template without version
                    name = templateName;
                    version = "";
                    return;
                }
                name = templateName.Substring(0, ix); // extract the template name
                version = templateName.Substring(ix + 1); // remainder is version
            }
        }
        // used to compare version numbers
        private class PackageFolderComparer : IComparer<string> {
            public int Compare(string? x, string? y) {
                string xVersion = Path.GetFileName(x) !;
                string yVersion = Path.GetFileName(y) !;
                return Package.CompareVersion(xVersion, yVersion);
            }
        }
    }
}
