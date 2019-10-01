/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using YetaWF.Core.IO;
using YetaWF.Core.Support;
using YetaWF.Core.Support.Zip;

namespace YetaWF.Core.Packages {

    public class SiteTemplateData {

        private List<string> PackageNames = new List<string> {
#if MVC6
            //"Softelvdm.IVR",
#endif
            "Softelvdm.TwilioProcessor",
            "YetaWF.AddThis",
            "YetaWF.Backups",
            "YetaWF.Basics",
            "YetaWF.Blog",
            "YetaWF.BootstrapCarousel",
            "YetaWF.Caching",
            "YetaWF.ComponentsHTML",
            "YetaWF.Core",
            "YetaWF.CurrencyConverter",
            "YetaWF.Dashboard",
            "YetaWF.DevTests",
            "YetaWF.Feed",
            "YetaWF.Feedback",
            "YetaWF.Identity",
            "YetaWF.IFrame",
            "YetaWF.ImageRepository",
            "YetaWF.Languages",
            "YetaWF.Lightbox",
            "YetaWF.Logging",
            "YetaWF.LoggingDataProvider",
            "YetaWF.Menus",
            "YetaWF.Messenger",
            "YetaWF.ModuleEdit",
            "YetaWF.Modules",
            "YetaWF.Packages",
            "YetaWF.PageEar",
            "YetaWF.PageEdit",
            "YetaWF.Pages",
            "YetaWF.Panels",
            "YetaWF.Scheduler",
            "YetaWF.Security",
            "YetaWF.SiteProperties",
            "YetaWF.Sites",
            "YetaWF.SyntaxHighlighter",
            "YetaWF.TawkTo",
            "YetaWF.Text",
            "YetaWF.TinyLanguage",
            "YetaWF.TinyLogin",
            "YetaWF.TwilioProcessor",
            "YetaWF.TwilioProcessorDataProvider",
            "YetaWF.UserProfile",
            "YetaWF.UserSettings",
            "YetaWF.Visitors",
        };

        public async Task MakeSiteTemplateDataAsync() {
            string rootFolder;
#if MVC6
            rootFolder = YetaWFManager.RootFolderWebProject;
#else
            rootFolder = YetaWFManager.RootFolder;
#endif
            string path = Path.Combine(rootFolder, Globals.SiteTemplates, "Data");
            // delete all existing zip files
            List<string> files = await FileSystem.FileSystemProvider.GetFilesAsync(path, "*.zip");
            foreach (string file in files) {
                await FileSystem.FileSystemProvider.DeleteFileAsync(file);
            }
            // export the data for each listed package and save the zip file in the site template data folder
            foreach (string packageName in PackageNames) {
                Package package = Package.GetPackageFromPackageName(packageName);
                using (YetaWFZipFile zipFile = await package.ExportDataAsync(takingBackup: true)) {
                    string file = Path.Combine(path, zipFile.FileName);
                    await zipFile.SaveAsync(file);
                }
            }
        }
    }
}
