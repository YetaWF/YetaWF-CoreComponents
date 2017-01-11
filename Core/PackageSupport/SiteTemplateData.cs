/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;
using System.IO;
using YetaWF.Core.Support;

namespace YetaWF.Core.Packages {
    public class SiteTemplateData {

        private List<string> PackageNames = new List<string> {
            "YetaWF.AddThis",
            "YetaWF.Backups",
            "YetaWF.Basics",
            "YetaWF.Blog",
            "YetaWF.BootstrapCarousel",
            "YetaWF.Core",
            "YetaWF.CurrencyConverter",
            "YetaWF.Dashboard",
            "YetaWF.DevTests",
            "YetaWF.Feed",
            "YetaWF.Feedback",
            "YetaWF.Identity",
            "YetaWF.IFrame",
            "YetaWF.ImageRepository",
            "YetaWF.KeepAlive",
            "YetaWF.Languages",
            "YetaWF.Lightbox",
            "YetaWF.Logging",
            "YetaWF.Menus",
            "YetaWF.ModuleEdit",
            "YetaWF.Modules",
            "YetaWF.Packages",
            "YetaWF.PageEar",
            "YetaWF.PageEdit",
            "YetaWF.Pages",
            "YetaWF.Scheduler",
            "YetaWF.Security",
            "YetaWF.SiteProperties",
            "YetaWF.Sites",
            "YetaWF.SlideShow",
            "YetaWF.SyntaxHighlighter",
            "YetaWF.Text",
            "YetaWF.TinyLanguage",
            "YetaWF.TinyLogin",
            "YetaWF.UserSettings",
            "YetaWF.Visitors",
        };

        public void MakeSiteTemplateData() {
            string path = Path.Combine(YetaWFManager.RootFolder, Globals.SiteTemplatesData);
            // delete all existing zip files
            string[] files = Directory.GetFiles(path, "*.zip");
            foreach (string file in files) {
                File.Delete(file);
            }
            // export the data for each listed package and save the zip file in the site template data folder
            foreach (string packageName in PackageNames) {
                Package package = Package.GetPackageFromPackageName(packageName);
                using (YetaWFZipFile zipFile = package.ExportData(takingBackup: true)) {
                    string file = Path.Combine(path, zipFile.Zip.Name);
                    zipFile.Zip.Save(file);
                }
            }
        }
    }
}
