/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YetaWF.Core.IO;
using YetaWF.Core.Support;
using YetaWF.Core.Support.Serializers;
using YetaWF.Core.Upload;

namespace YetaWF.Core.Packages {

    public partial class Package {

        //private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Package), name, defaultValue, parms); }

        public static async Task<bool> ImportDataAsync(string zipFileName, List<string> errorList) {

            string displayFileName = FileUpload.IsUploadedFile(zipFileName) ? __ResStr("uploadedDataFile", "Uploaded file") : Path.GetFileName(zipFileName);

            string xmlFile = null;

            using (ZipFile zip = ZipFile.Read(zipFileName)) {

                // check id file
                ZipEntry ze = zip[PackageIDDataFile];
                if (ze == null) {
                    errorList.Add(__ResStr("invDataFormat", "{0} is not a valid package data file.", displayFileName));
                    return false;
                }

                // read contents file
                xmlFile = Path.GetTempFileName();
                using (IFileStream fs = await FileSystem.TempFileSystemProvider.CreateFileStreamAsync(xmlFile)) {
                    ze = zip[PackageContentsFile];
                    ze.Extract(fs.GetFileStream());
                    await fs.CloseAsync();
                }
                SerializableData serData;
                using (IFileStream fs = await FileSystem.TempFileSystemProvider.OpenFileStreamAsync(xmlFile)) {
                    serData = (SerializableData)new GeneralFormatter(Package.ExportFormat).Deserialize(fs.GetFileStream());
                    await fs.CloseAsync();
                }
                await FileSystem.TempFileSystemProvider.DeleteFileAsync(xmlFile);

                // check if the originating package is really installed
                List<Package> allPackages = Package.GetAvailablePackages();
                Package realPackage = allPackages.Where(x => string.Compare(x.Name, serData.PackageName, StringComparison.OrdinalIgnoreCase) == 0).FirstOrDefault();
                if (realPackage == null) {
                    errorList.Add(__ResStr("errPkgReqDataImp", "Package {0} required to import data is not installed.", serData.PackageName));
                    return false;
                }
                if (Package.CompareVersion(realPackage.Version, serData.PackageVersion) < 0) {
                    errorList.Add(__ResStr("errPkgDataVers", "The data to be imported was created for version {0} of package {1}, but the installed package is older (version is {2}).",
                            serData.PackageVersion, realPackage.Name, realPackage.Version));
                    return false;
                }
                return await realPackage.ImportDataAsync(zip, displayFileName, serData, errorList);
            }
        }

        private async Task<bool> ImportDataAsync(ZipFile zip, string displayFileName, SerializableData serData, List<string> errorList) {

            // unzip all files/data
            foreach (var modelType in this.InstallableModels) {
                try {
                    object instMod = Activator.CreateInstance(modelType);
                    using ((IDisposable)instMod) {
                        IInstallableModel model = (IInstallableModel)instMod;
                        await model.RemoveSiteDataAsync();// remove site specific data so we can import the new data

                        // find the model data
                        SerializableModelData serModel = (from sd in serData.Data where sd.Class == modelType.Name select sd).FirstOrDefault();
                        if (serModel == null) // this model no longer exists
                            continue;

                        // unzip files - date provider doesn't have to do anything for files
                        foreach (var file in serModel.Files) {
                            ZipEntry e = zip[file.FileName];
                            if (YetaWFManager.HaveManager && file.SiteSpecific)
                                e.Extract(YetaWFManager.Manager.SiteFolder, ExtractExistingFileAction.OverwriteSilently);
                            else {
                                string rootFolder;
#if MVC6
                                rootFolder = YetaWFManager.RootFolderWebProject;
#else
                                rootFolder = YetaWFManager.RootFolder;
#endif
                                e.Extract(rootFolder, ExtractExistingFileAction.OverwriteSilently);
                            }
                        }

                        for (int chunk = 0; chunk < serModel.Chunks; ++chunk) {

                            // unzip data
                            {
                                string zipFileName = string.Format("{0}_{1}.xml", modelType.Name, chunk);
                                ZipEntry e = zip[zipFileName];
                                if (e == null) {
                                    errorList.Add(__ResStr("errDataCorrupt", "Zip file {1} corrupted - file {0} not found.", zipFileName, displayFileName));
                                    return false;
                                }

                                string xmlFile = Path.GetTempFileName();
                                using (IFileStream fs = await FileSystem.TempFileSystemProvider.CreateFileStreamAsync(xmlFile)) {
                                    e.Extract(fs.GetFileStream());
                                    await fs.CloseAsync();
                                }
                                object obj = null;
                                using (IFileStream fs = await FileSystem.TempFileSystemProvider.OpenFileStreamAsync(xmlFile)) {
                                    try {
                                        obj = new GeneralFormatter(Package.ExportFormat).Deserialize(fs.GetFileStream());
                                    } catch (Exception exc) {
                                        errorList.Add(__ResStr("errPkgDataDeser", "Error deserializing {0} - {1}", e.FileName, exc));
                                        return false;
                                    } finally {
                                        await fs.CloseAsync();
                                        await FileSystem.FileSystemProvider.DeleteFileAsync(xmlFile);
                                    }
                                }
                                await model.ImportChunkAsync(chunk, null, obj);
                            }
                        }
                    }
                } catch (Exception exc) {
                    errorList.Add(__ResStr("errDataCantImport", "Model type {0} cannot be imported - {1}", modelType.FullName, exc.Message));
                    return false;
                }
            }

            List<string> reqPackages = this.GetRequiredPackages();
            if (reqPackages.Count > 0) {
                string s = "";
                foreach (string reqPackage in reqPackages)
                    s += " " + reqPackage;
                errorList.Add(__ResStr("warnReqPckages", "This package requires additional packages - These should be imported at the same time:{0}", s));
            }
            return true;
        }
    }
}
