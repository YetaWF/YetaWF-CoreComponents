/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YetaWF.Core.IO;
using YetaWF.Core.Support;
using YetaWF.Core.Support.Serializers;
using YetaWF.Core.Upload;

namespace YetaWF.Core.Packages {

    public partial class Package {

        //private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Package), name, defaultValue, parms); }

        public static bool ImportData(string zipFileName, List<string> errorList) {

            string displayFileName = FileUpload.IsUploadedFile(zipFileName) ? __ResStr("uploadedDataFile", "Uploaded file") : Path.GetFileName(zipFileName);

            string xmlFile = null;

            using (ZipFile zip = ZipFile.Read(zipFileName)) {

                // check id file
                ZipEntry ze = zip[PackageIDDataFile];
                if (ze == null) {
                    errorList.Add(string.Format(__ResStr("invDataFormat", "{0} is not a valid package data file."), displayFileName));
                    return false;
                }

                // read contents file
                xmlFile = Path.GetTempFileName();
                FileStream fs = new FileStream(xmlFile, FileMode.Create, FileAccess.ReadWrite);
                ze = zip[PackageContentsFile];
                ze.Extract(fs);
                fs.Close();

                fs = new FileStream(xmlFile, FileMode.Open, FileAccess.Read);
                SerializableData serData = (SerializableData) new GeneralFormatter(Package.ExportFormat).Deserialize(fs);
                fs.Close();

                File.Delete(xmlFile);

                // check if the originating package is really installed
                List<Package> allPackages = Package.GetAvailablePackages();
                Package realPackage = allPackages.Where(x => string.Compare(x.Name, serData.PackageName, StringComparison.OrdinalIgnoreCase) == 0).FirstOrDefault();
                if (realPackage == null) {
                    errorList.Add(string.Format(__ResStr("errPkgReqDataImp", "Package {0} required to import data is not installed."), serData.PackageName));
                    return false;
                }
                if (Package.CompareVersion(realPackage.Version, serData.PackageVersion) < 0) {
                    errorList.Add(string.Format(__ResStr("errPkgDataVers", "The data to be imported was created for version {0} of package {1}, but the installed package is older (version is {2})."),
                            serData.PackageVersion, realPackage.Name, realPackage.Version));
                    return false;
                }
                return realPackage.ImportData(zip, displayFileName, serData, errorList);
            }
        }

        private bool ImportData(ZipFile zip, string displayFileName, SerializableData serData, List<string> errorList) {

            // unzip all files/data
            foreach (var modelType in this.InstallableModels) {
                try {
                    object instMod = Activator.CreateInstance(modelType);
                    using ((IDisposable)instMod) {
                        IInstallableModel model = (IInstallableModel)instMod;
                        model.RemoveSiteData();// remove site specific data so we can import the new data

                        // find the model data
                        SerializableModelData serModel = (from sd in serData.Data where sd.Class == modelType.Name select sd).FirstOrDefault();
                        if (serModel == null) // this model no longer exists
                            continue;

                        // unzip files - date provider doesn't have to do anything for files
                        foreach (var file in serModel.Files) {
                            ZipEntry e = zip[file.FileName];
                            if (YetaWFManager.HaveManager && file.SiteSpecific)
                                e.Extract(YetaWFManager.Manager.SiteFolder, ExtractExistingFileAction.OverwriteSilently);
                            else
                                e.Extract(YetaWFManager.RootFolder, ExtractExistingFileAction.OverwriteSilently);
                        }

                        for (int chunk = 0 ; chunk < serModel.Chunks ; ++chunk) {

                            // unzip data
                            {
                                string zipFileName = string.Format("{0}_{1}.xml", modelType.Name, chunk);
                                ZipEntry e = zip[zipFileName];
                                if (e == null) {
                                    errorList.Add(__ResStr("errDataCorrupt", "Zip file {1} corrupted - file {0} not found.", zipFileName, displayFileName));
                                    return false;
                                }

                                string xmlFile = Path.GetTempFileName();
                                FileStream fs = new FileStream(xmlFile, FileMode.Create, FileAccess.ReadWrite);
                                e.Extract(fs);
                                fs.Close();
                                fs = new FileStream(xmlFile, FileMode.Open, FileAccess.Read);
                                object obj = null;
                                try {
                                    obj = new GeneralFormatter(Package.ExportFormat).Deserialize(fs);
                                } catch (Exception exc) {
                                    errorList.Add(__ResStr("errPkgDataDeser", "Error deserializing {0} - {1}", e.FileName, exc));
                                    return false;
                                } finally {
                                    fs.Close();
                                    File.Delete(xmlFile);
                                }

                                model.ImportChunk(chunk, null, obj);
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
                errorList.Add(__ResStr("warnReqPckages", "This package requires additional packages - These should be imported at the same time:{0}",s ));
            }
            return true;
        }
    }
}
