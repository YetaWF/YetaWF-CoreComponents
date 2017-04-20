/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Ionic.Zip;
using System.IO;
using YetaWF.Core.Packages;
using YetaWF.Core.Support;
using YetaWF.Core.Support.Serializers;

namespace YetaWF.Core.Modules {
    public partial class ModuleDefinition {

        public const string ModuleContentsFile = "Contents.xml";
        public const string ModuleIDFile = "Module.txt";

        public YetaWFZipFile ExportData() {

            string zipName = __ResStr("moduleFmt", "Module Data - {0}.{1}.zip", this.ModuleDisplayName, this.Version);

            SerializableModule serModule;
            YetaWFZipFile zipFile = MakeZipFile(zipName, out serModule);

            // Add module definition
            serModule.ModuleGuid = this.ModuleGuid;
            serModule.ModDef = this;
            // and files (if any)
            serModule.Files = Package.ProcessAllFiles(this.ModuleDataFolder);

            // Add files
            foreach (var file in serModule.Files) {
                ZipEntry ze = zipFile.Zip.AddFile(file.AbsFileName);
                ze.FileName = file.FileName;
            }

            // serialize zipfile contents
            {
                string fileName = Path.GetTempFileName();
                zipFile.TempFiles.Add(fileName);

                FileStream fs = new FileStream(fileName, FileMode.Create);
                new GeneralFormatter(Package.ExportFormat).Serialize(fs, serModule);
                fs.Close();

                ZipEntry ze = zipFile.Zip.AddFile(fileName);
                ze.FileName = ModuleContentsFile;

                zipFile.Zip.AddEntry(ModuleIDFile, __ResStr("moduleData", "YetaWF Module Data"));
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
                Zip = new ZipFile(zipName),
            };
        }
    }
}
