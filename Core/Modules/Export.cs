/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.IO;
using System.Threading.Tasks;
using YetaWF.Core.IO;
using YetaWF.Core.Packages;
using YetaWF.Core.Support.Serializers;
using YetaWF.Core.Support.Zip;

namespace YetaWF.Core.Modules {

    public partial class ModuleDefinition {

        public const string ModuleContentsFile = "Contents.xml";
        public const string ModuleIDFile = "Module.txt";

        public async Task<YetaWFZipFile> ExportDataAsync() {

            string zipName = __ResStr("moduleFmt", "Module Data - {0}.{1}.zip", this.ModuleDisplayName, this.Version);

            SerializableModule serModule;
            YetaWFZipFile zipFile = MakeZipFile(zipName, out serModule);

            // Add module definition
            serModule.ModuleGuid = this.ModuleGuid;
            serModule.ModDef = this;
            // and files (if any)
            serModule.Files = await Package.ProcessAllFilesAsync(this.ModuleDataFolder);

            // Add files
            foreach (var file in serModule.Files) {
                zipFile.AddFile(file.AbsFileName, file.FileName);
            }

            // serialize zipfile contents
            {
                string fileName = Path.GetTempFileName();
                zipFile.TempFiles.Add(fileName);

                using (IFileStream fs = await FileSystem.FileSystemProvider.CreateFileStreamAsync(fileName)) {
                    new GeneralFormatter(Package.ExportFormat).Serialize(fs.GetFileStream(), serModule);
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
