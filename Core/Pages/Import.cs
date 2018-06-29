/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using YetaWF.Core.IO;
using YetaWF.Core.Modules;
using YetaWF.Core.Packages;
using YetaWF.Core.Support;
using YetaWF.Core.Support.Serializers;
using YetaWF.Core.Support.Zip;
using YetaWF.Core.Upload;

namespace YetaWF.Core.Pages {

    public partial class PageDefinition {

        /* private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(PageDefinition), name, defaultValue, parms); } */

        private static readonly string ModuleContentsFile = "Contents.xml";

        public class ImportInfo {
            public bool Success { get; set; }
            public string Url { get; set; }
        }

        public static async Task<ImportInfo> ImportAsync(string zipFileName, List<string> errorList) {

            ImportInfo info = new ImportInfo();
            string displayFileName = FileUpload.IsUploadedFile(zipFileName) ? __ResStr("uploadedFile", "Uploaded file") : Path.GetFileName(zipFileName);

            string xmlFile = null;

            using (ZipFile zip = new ZipFile(zipFileName)) {

                // check id file
                ZipEntry ze = zip.GetEntry(PageIDFile);
                if (ze == null) {
                    errorList.Add(__ResStr("invFormat", "{0} is not valid page data", displayFileName));
                    return info;
                }

                // read contents file
                xmlFile = Path.GetTempFileName();
                using (IFileStream fs = await FileSystem.TempFileSystemProvider.CreateFileStreamAsync(xmlFile)) {
                    ze = zip.GetEntry(PageContentsFile);
                    using (Stream entryStream = zip.GetInputStream(ze)) {
                        Extract(entryStream, fs);
                    }
                    await fs.CloseAsync();
                }
                SerializablePage serPage;
                using (IFileStream fs = await FileSystem.TempFileSystemProvider.OpenFileStreamAsync(xmlFile)) {
                    serPage = (SerializablePage)new GeneralFormatter(Package.ExportFormat).Deserialize(fs.GetFileStream());
                    await fs.CloseAsync();
                }
                await FileSystem.TempFileSystemProvider.DeleteFileAsync(xmlFile);

                if (Package.CompareVersion(YetaWF.Core.Controllers.AreaRegistration.CurrentPackage.Version, serPage.CoreVersion) < 0) {
                    errorList.Add(__ResStr("invCore", "This page requires YetaWF version {0} - Current version found is {1}", serPage.CoreVersion, YetaWF.Core.Controllers.AreaRegistration.CurrentPackage.Version));
                    return info;
                }
                return await ImportAsync(zip, displayFileName, serPage, errorList);
            }
        }

        private static async Task<ImportInfo> ImportAsync(ZipFile zip, string displayFileName, SerializablePage serPage, List<string> errorList) {
            ImportInfo info = new ImportInfo();
            // unzip all files/data
            try {
                PageDefinition pageDef = serPage.PageDef;
                NewPageInfo newPage = await PageDefinition.CreateNewPageAsync(pageDef.Title, pageDef.Description, pageDef.Url, null, false);
                if (newPage.Page == null)
                    throw new Error(newPage.Message);
                foreach (string modZipFile in serPage.ModuleZips) {
                    await ImportModuleAsync(zip, displayFileName, serPage, modZipFile, errorList);
                }
                // save the page
                pageDef.Temporary = false;
                pageDef.PageGuid = newPage.Page.PageGuid;
                pageDef.AllowedUsers = newPage.Page.AllowedUsers;// set default authorizations (Admin only)
                pageDef.AllowedRoles = newPage.Page.AllowedRoles;
                await pageDef.SaveAsync();
                info.Success = true;
                info.Url = pageDef.Url;
                return info;
            } catch (Exception exc) {
                errorList.Add(__ResStr("errCantImport", "Page {0} cannot be imported - {1}", serPage.PageUrl, ErrorHandling.FormatExceptionMessage(exc)));
                return info;
            }
        }

        private static async Task<ModuleDefinition> ImportModuleAsync(ZipFile zip, string displayFileName, SerializablePage serPage, string modZipFile, List<string> errorList) {

            // read module zip file
            string modZipFileName = Path.GetTempFileName();
            using (IFileStream fs = await FileSystem.TempFileSystemProvider.CreateFileStreamAsync(modZipFileName)) {
                ZipEntry ze = zip.GetEntry(modZipFile);
                using (Stream entryStream = zip.GetInputStream(ze)) {
                    Extract(entryStream, fs);
                }
                await fs.CloseAsync();
            }

            // open the module zip file
            SerializableModule serModule;
            using (ZipFile modZip = new ZipFile(modZipFileName)) {

                // read contents file
                string xmlFile = Path.GetTempFileName();
                using (IFileStream fs = await FileSystem.TempFileSystemProvider.CreateFileStreamAsync(xmlFile)) {
                    ZipEntry ze = modZip.GetEntry(ModuleContentsFile);
                    using (Stream entryStream = modZip.GetInputStream(ze)) {
                        Extract(entryStream, fs);
                    }
                    await fs.CloseAsync();
                }
                using (IFileStream fs = await FileSystem.TempFileSystemProvider.OpenFileStreamAsync(xmlFile)) {
                    serModule = (SerializableModule)new GeneralFormatter(Package.ExportFormat).Deserialize(fs.GetFileStream());
                    await fs.CloseAsync();
                }
                await FileSystem.TempFileSystemProvider.DeleteFileAsync(xmlFile);

                // save the module
                ModuleDefinition modExisting = await ModuleDefinition.LoadAsync(serModule.ModDef.ModuleGuid, AllowNone: true);
                if (modExisting == null) {

                    serModule.ModDef.Temporary = false;
                    await serModule.ModDef.SaveAsync();

                    // unzip ALL files
                    foreach (var file in serModule.Files) {
                        ZipEntry e = modZip.GetEntry(YetaWFZipFile.CleanFileName(file.FileName));
                        if (file.SiteSpecific) {
                            string fName = file.FileName;
                            fName = Manager.SiteFolder + fName;
                            await FileSystem.FileSystemProvider.CreateDirectoryAsync(Path.GetDirectoryName(fName));
                            using (IFileStream fs = await FileSystem.FileSystemProvider.CreateFileStreamAsync(fName)) {
                                using (Stream entryStream = modZip.GetInputStream(e)) {
                                    Extract(entryStream, fs);
                                }
                                await fs.CloseAsync();
                            }
                        } else {
                            throw new Error(__ResStr("nonSite", "Module data {0} cannot be imported - It contains unexpected non site specific data", serModule.ModuleName));
                        }
                    }
                } else {
                    errorList.Add(__ResStr("modExists", "Module {0} already exists and has not been imported (Module Guid {1})", modExisting.Name, modExisting.ModuleGuid));
                }
            }
            await FileSystem.TempFileSystemProvider.DeleteFileAsync(modZipFileName);

            return serModule.ModDef;
        }

        private static void Extract(Stream entryStream, IFileStream fs) {
            byte[] buffer = new byte[4096];
            StreamUtils.Copy(entryStream, fs.GetFileStream(), buffer);
        }
    }
}
