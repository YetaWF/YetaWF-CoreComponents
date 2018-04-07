/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetaWF.Core.IO;
using YetaWF.Core.Log;
using YetaWF.Core.Site;
using YetaWF.Core.Support;

namespace YetaWF.Core.Packages {

    public partial class Package {

        private class PackageInfo {
            public string Name { get; set; }
            public string Version { get; set; }
        }

        public static bool MajorDataChange = false;

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
            string rootFolder;
#if MVC6
            rootFolder = YetaWFManager.RootFolderWebProject;
#else
            rootFolder = YetaWFManager.RootFolder;
#endif
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
            if (YetaWFManager.Manager.Deployed && !MustUpgrade()) return;

            //File.Delete(Path.Combine(YetaWFManager.RootFolder, Globals.DataFolder, Globals.UpgradeLogFile));

            // Create an update log file
            UpgradeLogging log = new UpgradeLogging();
            Logging.RegisterLogging(log);

            SiteDefinition site = await SiteDefinition.LoadSiteDefinitionAsync(null);// get the default site
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
        public class UpgradeLogging : ILogging {
            public Logging.LevelEnum GetLevel() { return Logging.LevelEnum.Info; }
            public Task InitAsync() { return Task.CompletedTask; }
            public Task ClearAsync() { return Task.CompletedTask; }
            public Task FlushAsync() { return Task.CompletedTask; }
            public Task<bool> IsInstalledAsync() { return Task.FromResult(true); }
            public void WriteToLogFile(string category, Logging.LevelEnum level, int relStack, string text) {
                string rootFolder;
#if MVC6
                rootFolder = YetaWFManager.RootFolderWebProject;
#else
                rootFolder = YetaWFManager.RootFolder;
#endif
                FileSystem.FileSystemProvider.AppendAllTextAsync(Path.Combine(rootFolder, Globals.DataFolder, Globals.UpgradeLogFile), text + "\r\n").Wait();// uhm yeah, only while upgrading
            }
        }
        /// <summary>
        /// Returns whether an upgrade is force (even on deployed systems)
        /// </summary>
        /// <returns></returns>
        public static bool MustUpgrade() {
            return FileSystem.FileSystemProvider.FileExistsAsync(GetUpdateIndicatorFileName()).Result;// uhm yeah, only while upgrading
        }
        private static string GetUpdateIndicatorFileName() {
            string rootFolder;
#if MVC6
            rootFolder = YetaWFManager.RootFolderWebProject;
#else
            rootFolder = YetaWFManager.RootFolder;
#endif
            return Path.Combine(rootFolder, Globals.UpdateIndicatorFile);
        }

        private static async Task UpgradePackagesAsync() {

            Logging.AddLog("Upgrading to new packages");

            List<PackageInfo> list = await LoadPackageMapAsync();

            // get all currently installed packages
            List<Package> allPackages = Package.GetAvailablePackages();

            PackageInfo coreInfo = (from p in list where p.Name == "YetaWF.Core" select p).FirstOrDefault();
            if (coreInfo == null)
                throw new InternalError("YetaWF.Core package not found in package map");
            //bool dropIndexes = Package.CompareVersion(coreInfo.Version, "2.0.3") < 0; // we're upgrading from pre-2.0.3, need to upgrade all varchar columns -> drop all indexes
            bool dropIndexes = Package.CompareVersion(coreInfo.Version, "2.8.0") < 0; // we're upgrading from pre-2.8.0, need to upgrade for new SQL data provider
            MajorDataChange = dropIndexes;

            // update/create all models
            if (dropIndexes || MustUpgrade()) {
                Logging.AddLog("Updating all packages");
                await UpdateAllAsync();
                Logging.AddLog("Updating models for all packages completed");
            } else {
                // update models
                Logging.AddLog("Updating models for changed packages");
                foreach (Package package in allPackages) {
                    PackageInfo info = (from p in list where p.Name == package.Name select p).FirstOrDefault();
                    string lastSeenVersion;
                    if (info == null) {
                        // brand new package
                        Logging.AddLog("New package {0}", package.Name);
                        lastSeenVersion = "";
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
                PackageInfo info = (from p in list where p.Name == package.Name select p).FirstOrDefault();
                if (info == null) {
                    // new package
                    await InstallSiteTemplateAsync(package, null);
                } else {
                    int cmp = Package.CompareVersion(info.Version, package.Version);
                    if (cmp < 0) {
                        // upgraded package
                        await InstallSiteTemplateAsync(package, info.Version);
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
        }
        /// <summary>
        /// Loads the existing package map at .\Website\Data\PackageMap.txt
        /// </summary>
        /// <returns>Information for all packages that were available during the last startup of YetaWF.</returns>
        private static async Task<List<PackageInfo>> LoadPackageMapAsync() {
            List<PackageInfo> list = new List<PackageInfo>();
            string rootFolder;
#if MVC6
            rootFolder = YetaWFManager.RootFolderWebProject;
#else
            rootFolder = YetaWFManager.RootFolder;
#endif
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
        private static async Task InstallPackageAsync(Package package, string lastSeenVersion = null) {
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
        private static async Task InstallSiteTemplateAsync(Package package, string lastSeenVersion) {
            string rootFolder;
#if MVC6
            rootFolder = YetaWFManager.RootFolderWebProject;
#else
            rootFolder = YetaWFManager.RootFolder;
#endif
            string templateBase = package.Name.Replace(".", "_");
            string templateFolder = Path.Combine(rootFolder, Globals.SiteTemplates);
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
                    Func<QueryHelper, Task> action = await BuiltinCommands.FindAsync("/$processtemplate", checkAuthorization: false);
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
            public int Compare(string x, string y) {
                // each site template is in the following form:
                // package_name.txt  or
                // package_name.version.txt where version is x.y.z
                // so we need to parse the file name to extract the version and compare name/version accordingly
                string xName, xVersion;
                SiteTemplateNameComparer.GetComponents(x, out xName, out xVersion);
                string yName, yVersion;
                SiteTemplateNameComparer.GetComponents(y, out yName, out yVersion);
                int iName = string.Compare(xName, yName);
                if (iName != 0) return iName;// the names aren't the same
                // same name, so check version
                return Package.CompareVersion(xVersion, yVersion);
            }
            public static void GetComponents(string templateName, out string name, out string version) {
                int ix = templateName.IndexOf(".");
                if (ix < 0) { // template without version
                    name = templateName;
                    version = "";
                    return;
                }
                name = templateName.Substring(0, ix); // extract the template name
                version = templateName.Substring(ix + 1); // remainder is version
            }
        }
    }
}
