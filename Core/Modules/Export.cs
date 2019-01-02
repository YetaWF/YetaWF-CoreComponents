/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YetaWF.Core.Identity;
using YetaWF.Core.IO;
using YetaWF.Core.Packages;
using YetaWF.Core.Support.Serializers;
using YetaWF.Core.Support.Zip;

namespace YetaWF.Core.Modules {

    public partial class ModuleDefinition {

        public const string ModuleContentsFile = "Contents.json";
        public const string ModuleIDFile = "Module.txt";

        /* private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(ModuleDefinition), name, defaultValue, parms); } */

        public async Task<YetaWFZipFile> ExportDataAsync() {

            string zipName = __ResStr("moduleFmt", "Module Data - {0}.{1}.zip", this.ModuleDisplayName, this.Version);

            SerializableModule serModule;
            YetaWFZipFile zipFile = MakeZipFile(zipName, out serModule);

            // Add module definition
            serModule.ModuleGuid = this.ModuleGuid;
            serModule.ModDef = this;
            // and files (if any)
            serModule.Files = await Package.ProcessAllFilesAsync(this.ModuleDataFolder);
            // add roles (we need to save role names in case we restore on another site with different role Ids)
            serModule.Roles = new Serializers.SerializableList<RoleLookupEntry>(
                (from r in Resource.ResourceAccess.GetDefaultRoleList() select new RoleLookupEntry {
                    RoleId = r.RoleId,
                    RoleName = r.Name,
                }));
            // add users
            foreach (AllowedUser user in this.AllowedUsers) {
                string name = await Resource.ResourceAccess.GetUserNameAsync(user.UserId);
                serModule.Users.Add(new UserLookupEntry {
                    UserId = user.UserId,
                    UserName = name,
                });
            }

            // Add files
            foreach (var file in serModule.Files) {
                zipFile.AddFile(file.AbsFileName, file.FileName);
            }

            // serialize zipfile contents
            {
                string fileName = Path.GetTempFileName();
                zipFile.TempFiles.Add(fileName);

                using (IFileStream fs = await FileSystem.FileSystemProvider.CreateFileStreamAsync(fileName)) {
                    new GeneralFormatter(Package.ExportFormatModules).Serialize(fs.GetFileStream(), serModule);
                    await fs.CloseAsync();
                }
                zipFile.AddFile(fileName, ModuleContentsFile);

                zipFile.AddData("YetaWF Module Data", ModuleIDFile);
            }
            return zipFile;
        }

        private YetaWFZipFile MakeZipFile(string zipName, out SerializableModule serModule) {
            serModule = new SerializableModule();
            serModule.ModuleName = this.ModuleDisplayName;
            serModule.ModuleVersion = this.Version;
            serModule.CoreVersion = YetaWF.Core.Controllers.AreaRegistration.CurrentPackage.Version;

            return new YetaWFZipFile {
                FileName = zipName,
            };
        }
    }
}
