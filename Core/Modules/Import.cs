/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using YetaWF.Core.Packages;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;
using YetaWF.Core.Support.Serializers;
using YetaWF.Core.Upload;

namespace YetaWF.Core.Modules {
    public partial class ModuleDefinition {

        public static bool Import(string zipFileName, Guid pageGuid, bool newModule, string pane, bool top, List<string> errorList) {

            string displayFileName = FileUpload.IsUploadedFile(zipFileName) ? __ResStr("uploadedFile", "Uploaded file") : Path.GetFileName(zipFileName);

            string xmlFile = null;

            using (ZipFile zip = ZipFile.Read(zipFileName)) {

                // check id file
                ZipEntry ze = zip[ModuleIDFile];
                if (ze == null) {
                    errorList.Add(__ResStr("invFormat", "{0} is not valid module data", displayFileName));
                    return false;
                }

                // read contents file
                xmlFile = Path.GetTempFileName();
                FileStream fs = new FileStream(xmlFile, FileMode.Create, FileAccess.ReadWrite);
                ze = zip[ModuleContentsFile];
                ze.Extract(fs);
                fs.Close();
                fs = new FileStream(xmlFile, FileMode.Open, FileAccess.Read);
                SerializableModule serModule = (SerializableModule)new GeneralFormatter(Package.ExportFormat).Deserialize(fs);
                fs.Close();
                File.Delete(xmlFile);

                if (Package.CompareVersion(YetaWF.Core.Controllers.AreaRegistration.CurrentPackage.Version, serModule.CoreVersion) < 0) {
                    errorList.Add(__ResStr("invCore", "This module requires YetaWF version {0} - Current version found is {1}", serModule.CoreVersion, YetaWF.Core.Controllers.AreaRegistration.CurrentPackage.Version));
                    return false;
                }
                return Import(zip, displayFileName, serModule, pageGuid, newModule, pane, top, errorList);
            }
        }

        private static bool Import(ZipFile zip, string displayFileName, SerializableModule serModule, Guid pageGuid, bool newModule, string pane, bool top, List<string> errorList) {
            // unzip all files/data
            try {
                ModuleDefinition modDef = serModule.ModDef;
                Guid originalGuid = modDef.ModuleGuid;
                if (newModule) {
                    // new module
                    if (serModule.ModDef.IsModuleUnique)
                        throw new Error(__ResStr("unique", "Module {0} is a unique module and can't be imported as a new module", serModule.ModDef.Name));

                    PageDefinition page = PageDefinition.Load(pageGuid);
                    if (page == null)
                        throw new Error(__ResStr("pageNotFound", "Page with id {0} doesn't exist", pageGuid));

                    modDef.ModuleGuid = Guid.NewGuid();
                    modDef.Temporary = false;
                    serModule.ModDef.Save(); // save as new module

                    page.AddModule(pane, modDef, top);
                    page.Save();
                } else {
                    // replace existing
                    //RESEARCH: do we really need this?
                }
                // unzip ALL files but replace guid if it's part of the path
                foreach (var file in serModule.Files) {
                    ZipEntry e = zip[file.FileName];
                    if (HaveManager && file.SiteSpecific) {
                        string fName = file.FileName;
                        fName = fName.Replace(originalGuid.ToString(), modDef.ModuleGuid.ToString());
                        fName = Manager.SiteFolder + fName;
                        Directory.CreateDirectory(Path.GetDirectoryName(fName));
                        FileStream fs = new FileStream(fName, FileMode.Create, FileAccess.ReadWrite);
                        e.Extract(fs);
                        fs.Close();

                        // open file and replace old module guid with new module guid (this is mostly in case module has guid embedded in data)
                        string contents = File.ReadAllText(fName);
                        string newContents = contents.Replace(originalGuid.ToString(), modDef.ModuleGuid.ToString());
                        if (contents != newContents)
                            File.WriteAllText(fName, newContents);
                    } else {
                        throw new Error(__ResStr("nonSite", "Module Data {0} cannot be imported - It contains unexpected non site specific data", serModule.ModuleName));
                    }
                }
            } catch (Exception exc) {
                errorList.Add(__ResStr("errCantImport", "Module Data {0}({1}) cannot be imported - {2}", serModule.ModuleName, serModule.ModuleVersion, exc.Message));
                return false;
            }
            return true;
        }
    }
}
