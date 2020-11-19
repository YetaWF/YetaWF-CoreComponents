/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Linq;
using System.Threading.Tasks;
using YetaWF.Core.Identity;
using YetaWF.Core.IO;
using YetaWF.Core.Modules;
using YetaWF.Core.Packages;
using YetaWF.Core.Support.Serializers;
using YetaWF.Core.Support.Zip;

namespace YetaWF.Core.Pages {

    public partial class PageDefinition {

        /* private static string __ResStr(string name, string defaultValue, params object?[] parms) { return ResourceAccess.GetResourceString(typeof(PageDefinition), name, defaultValue, parms); } */

        public const string PageContentsFile = "Contents.json";
        public const string PageIDFile = "Page.txt";

        public async Task<YetaWFZipFile> ExportAsync() {

            string zipName = __ResStr("moduleFmt", "Page Data - {0}.zip", this.Url);

            SerializablePage serPage;
            YetaWFZipFile zipFile = MakeZipFile(zipName, out serPage);

            // Add page definition
            serPage.PageGuid = this.PageGuid;
            serPage.PageDef = this;

            // add roles (we need to save role names in case we restore on another site with different role Ids)
            serPage.Roles = new Serializers.SerializableList<RoleLookupEntry>(
                (from r in Resource.ResourceAccess.GetDefaultRoleList() select new RoleLookupEntry {
                    RoleId = r.RoleId,
                    RoleName = r.Name,
                }));
            // add users
            foreach (AllowedUser user in this.AllowedUsers) {
                string name = await Resource.ResourceAccess.GetUserNameAsync(user.UserId);
                serPage.Users.Add(new UserLookupEntry {
                    UserId = user.UserId,
                    UserName = name,
                });
            }

            // Add modules
            foreach (ModuleEntry modEntry in this.ModuleDefinitions) {
                ModuleDefinition? mod = await modEntry.GetModuleAsync();
                if (mod != null) {
                    // export the module
                    YetaWFZipFile modZip = await mod.ExportDataAsync();
                    // save the module zip file to a temp file
                    string modZipFileName = FileSystem.TempFileSystemProvider.GetTempFile();
                    zipFile.TempFiles.Add(modZipFileName);
                    await modZip.SaveAsync(modZipFileName);
                    // add the module zip file to the page zip file
                    serPage.ModuleZips.Add(mod.ModuleGuidName + ".zip");
                    zipFile.AddFile(modZipFileName, mod.ModuleGuidName + ".zip");
                }
            }

            // serialize zipfile contents
            {
                string fileName = FileSystem.TempFileSystemProvider.GetTempFile();
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
            serPage.CoreVersion = YetaWF.Core.AreaRegistration.CurrentPackage.Version;

            return new YetaWFZipFile {
                FileName = zipName,
            };
        }
    }
}
