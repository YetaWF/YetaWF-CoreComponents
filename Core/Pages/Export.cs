/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.IO;
using System.Threading.Tasks;
using YetaWF.Core.IO;
using YetaWF.Core.Modules;
using YetaWF.Core.Packages;
using YetaWF.Core.Support.Serializers;
using YetaWF.Core.Support.Zip;

namespace YetaWF.Core.Pages {

    public partial class PageDefinition {

        /* private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(PageDefinition), name, defaultValue, parms); } */

        public const string PageContentsFile = "Contents.json";
        public const string PageIDFile = "Page.txt";

        public async Task<YetaWFZipFile> ExportAsync() {

            string zipName = __ResStr("moduleFmt", "Page Data - {0}.zip", this.Url);

            SerializablePage serPage;
            YetaWFZipFile zipFile = MakeZipFile(zipName, out serPage);

            // Add page definition
            serPage.PageGuid = this.PageGuid;
            serPage.PageDef = this;

            // Add modules
            foreach (ModuleEntry modEntry in this.ModuleDefinitions) {
                ModuleDefinition mod = await modEntry.GetModuleAsync();
                if (mod != null) {
                    // export the module
                    YetaWFZipFile modZip = await mod.ExportDataAsync();
                    // save the module zip file to a temp file
                    string modZipFileName = Path.GetTempFileName();
                    await modZip.SaveAsync(modZipFileName);
                    await modZip.CleanupFoldersAsync();
                    // add the module zip file to the page zip file
                    serPage.ModuleZips.Add(mod.ModuleGuidName);
                    zipFile.AddFile(modZipFileName, mod.ModuleGuidName);
                    zipFile.TempFiles.Add(modZipFileName);
                }
            }

            // serialize zipfile contents
            {
                string fileName = Path.GetTempFileName();
                zipFile.TempFiles.Add(fileName);

                using (IFileStream fs = await FileSystem.FileSystemProvider.CreateFileStreamAsync(fileName)) {
                    new GeneralFormatter(Package.ExportFormat).Serialize(fs.GetFileStream(), serPage);
                    await fs.CloseAsync();
                }
                zipFile.AddFile(fileName, PageContentsFile);

                zipFile.AddData("YetaWF Page Data", PageIDFile);
            }
            return zipFile;
        }

        private YetaWFZipFile MakeZipFile(string zipName, out SerializablePage serPage) {
            serPage = new SerializablePage();
            serPage.PageUrl = this.Url;
            serPage.CoreVersion = YetaWF.Core.Controllers.AreaRegistration.CurrentPackage.Version;

            return new YetaWFZipFile {
                FileName = zipName,
            };
        }
    }
}
