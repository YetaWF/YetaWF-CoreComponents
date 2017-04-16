/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Ionic.Zip;
using System;
using System.IO;
using YetaWF.Core.IO;
using YetaWF.Core.Serializers;
using YetaWF.Core.Support;
using YetaWF.Core.Support.Serializers;

namespace YetaWF.Core.Packages {

    public partial class Package {

        //private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Package), name, defaultValue, parms); }

        public YetaWFZipFile ExportData(bool takingBackup = false) {

            if (!IsModulePackage && !IsCorePackage)
                throw new InternalError("This package type has no associated data to export");

            string zipName = string.Format(__ResStr("packageDataFmt", "Package Data - {0}.{1}.zip", this.Name, this.Version));

            SerializableData serData;
            ZipEntry ze;
            FileStream fs;
            string fileName;
            YetaWFZipFile zipFile = MakeZipFile(zipName, out serData);

            serData.Data = new SerializableList<SerializableModelData>();

            // Export all models with data this package implements
            foreach (var modelType in this.InstallableModels) {
                try {
                    object instMod = Activator.CreateInstance(modelType);
                    using ((IDisposable)instMod) {
                        IInstallableModel model = (IInstallableModel) instMod;
                        if (!takingBackup || model.IsInstalled()) {
                            SerializableModelData serModel = new SerializableModelData();

                            bool more = true;
                            int chunk = 0;
                            for (; more ; ++chunk) {
                                SerializableList<SerializableFile> fileList = new SerializableList<SerializableFile>();
                                object obj = null;
                                more = model.ExportChunk(chunk, fileList, out obj);

                                if (fileList != null && fileList.Count > 0) {
                                    serModel.Files.AddRange(fileList);
                                    foreach (var file in fileList) {
                                        ze = zipFile.Zip.AddFile(file.AbsFileName);
                                        ze.FileName = file.FileName;
                                    }
                                }
                                if (!more && obj == null)
                                    break;

                                fileName = Path.GetTempFileName();
                                zipFile.TempFiles.Add(fileName);

                                fs = new FileStream(fileName, FileMode.Create);
                                new GeneralFormatter(Package.ExportFormat).Serialize(fs, obj);
                                fs.Close();

                                ze = zipFile.Zip.AddFile(fileName);
                                ze.FileName = string.Format("{0}_{1}.xml", modelType.Name, chunk);
                            }

                            serModel.Class = modelType.Name;
                            serModel.Chunks = chunk;

                            serData.Data.Add(serModel);
                        }
                    }
                } catch (Exception exc) {
                    throw new Error(__ResStr("errModCantExport", "Model type {0} cannot be exported - {1}"), modelType.FullName, exc.Message);
                }
            }

            // serialize package contents
            fileName = Path.GetTempFileName();
            zipFile.TempFiles.Add(fileName);
            fs = new FileStream(fileName, FileMode.Create);
            new GeneralFormatter(Package.ExportFormat).Serialize(fs, serData);
            fs.Close();
            ze = zipFile.Zip.AddFile(fileName);
            ze.FileName = PackageContentsFile;

            zipFile.Zip.AddEntry(PackageIDDataFile, __ResStr("packageData", "YetaWF Package Data"));

            return zipFile;
        }

        private YetaWFZipFile MakeZipFile(string zipName, out SerializableData serData) {
            serData = new SerializableData();
            serData.PackageName = this.Name;
            serData.PackageVersion = this.Version;

            return new YetaWFZipFile {
                FileName = zipName,
                Zip = new ZipFile(zipName),
            };
        }
    }
}
