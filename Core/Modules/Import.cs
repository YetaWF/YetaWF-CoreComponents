/* Copyright © 2021 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YetaWF.Core.Identity;
using YetaWF.Core.IO;
using YetaWF.Core.Packages;
using YetaWF.Core.Pages;
using YetaWF.Core.Serializers;
using YetaWF.Core.Support;
using YetaWF.Core.Support.Serializers;
using YetaWF.Core.Support.Zip;
using YetaWF.Core.Upload;

namespace YetaWF.Core.Modules {

    public partial class ModuleDefinition {

        /* private static string __ResStr(string name, string defaultValue, params object?[] parms) { return ResourceAccess.GetResourceString(typeof(ModuleDefinition), name, defaultValue, parms); } */

        public static async Task<bool> ImportAsync(string zipFileName, Guid pageGuid, bool newModule, string pane, bool top, List<string> errorList) {

            string displayFileName = FileUpload.IsUploadedFile(zipFileName) ? __ResStr("uploadedFile", "Uploaded file") : Path.GetFileName(zipFileName);

            using (ZipFile zip = new ZipFile(zipFileName)) {

                // check id file
                ZipEntry? ze = zip.GetEntry(ModuleIDFile);
                if (ze == null) {
                    errorList.Add(__ResStr("invFormat", "{0} is not valid module data", displayFileName));
                    return false;
                }

                // read contents file
                string jsonFile = FileSystem.TempFileSystemProvider.GetTempFile();
                using (IFileStream fs = await FileSystem.TempFileSystemProvider.CreateFileStreamAsync(jsonFile)) {
                    ze = zip.GetEntry(ModuleContentsFile);
                    using (Stream entryStream = zip.GetInputStream(ze)) {
                        Extract(entryStream, fs);
                    }
                    await fs.CloseAsync();
                }
                SerializableModule serModule;
                using (IFileStream fs = await FileSystem.TempFileSystemProvider.OpenFileStreamAsync(jsonFile)) {
                    serModule = new GeneralFormatter(Package.ExportFormatModules).Deserialize<SerializableModule>(fs.GetFileStream());
                    await fs.CloseAsync();
                }
                await FileSystem.TempFileSystemProvider.DeleteFileAsync(jsonFile);

                if (Package.CompareVersion(YetaWF.Core.AreaRegistration.CurrentPackage.Version, serModule.CoreVersion) < 0) {
                    errorList.Add(__ResStr("invCore", "This module requires YetaWF version {0} - Current version found is {1}", serModule.CoreVersion, YetaWF.Core.AreaRegistration.CurrentPackage.Version));
                    return false;
                }

                await UpdateRolesAndUsers(serModule);

                return await ImportAsync(zip, displayFileName, serModule, pageGuid, newModule, pane, top, errorList);
            }
        }

        internal static async Task UpdateRolesAndUsers(SerializableModule serModule) {
            // Update all roles with the role id based on the role name (ids are different between sites)
            serModule.ModDef.AllowedRoles = await GetUpdatedRolesAsync(serModule.ModDef.AllowedRoles, serModule.Roles);
            // Update all users with the user id based on the user name (ids are different between sites)
            serModule.ModDef.AllowedUsers = await GetUpdatedUsersAsync(serModule.ModDef.AllowedUsers, serModule.Users);
        }
        internal static Task<SerializableList<AllowedRole>> GetUpdatedRolesAsync(List<AllowedRole> origRoles, SerializableList<RoleLookupEntry> lookupEntries) {
            SerializableList<AllowedRole> allowedRoles = new SerializableList<AllowedRole>();
            foreach (AllowedRole origRole in origRoles) {
                string? roleName = (from r in lookupEntries where r.RoleId == origRole.RoleId select r.RoleName).FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(roleName)) {
                    int roleId = 0;
                    try {
                        roleId = Resource.ResourceAccess.GetRoleId(roleName);
                    } catch (Exception) { }
                    if (roleId != 0) {
                        origRole.RoleId = roleId;
                        allowedRoles.Add(origRole);
                    }
                }
            }
            return Task.FromResult(allowedRoles);
        }
        internal static async Task<SerializableList<AllowedUser>> GetUpdatedUsersAsync(List<AllowedUser> origUsers, SerializableList<UserLookupEntry> lookupEntries) {
            SerializableList<AllowedUser> allowedUsers = new SerializableList<AllowedUser>();
            foreach (AllowedUser origUser in origUsers) {
                string? userName = (from r in lookupEntries where r.UserId == origUser.UserId select r.UserName).FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(userName)) {
                    int userId = 0;
                    try {
                        userId = await Resource.ResourceAccess.GetUserIdAsync(userName);
                    } catch (Exception) { }
                    if (userId != 0) {
                        origUser.UserId = userId;
                        allowedUsers.Add(origUser);
                    }
                }
            }
            return allowedUsers;
        }

        private static async Task<bool> ImportAsync(ZipFile zip, string displayFileName, SerializableModule serModule, Guid pageGuid, bool newModule, string pane, bool top, List<string> errorList) {
            // unzip all files/data
            try {
                ModuleDefinition modDef = serModule.ModDef;
                Guid originalGuid = modDef.ModuleGuid;
                if (newModule) {
                    // new module
                    if (serModule.ModDef.IsModuleUnique)
                        throw new Error(__ResStr("unique", "Module {0} is a unique module and can't be imported as a new module", serModule.ModDef.Name));

                    PageDefinition page = await PageDefinition.LoadAsync(pageGuid);
                    if (page == null)
                        throw new Error(__ResStr("pageNotFound", "Page with id {0} doesn't exist", pageGuid));

                    modDef.ModuleGuid = Guid.NewGuid();
                    modDef.Temporary = false;
                    await serModule.ModDef.SaveAsync(); // save as new module

                    page.AddModule(pane, modDef, top);
                    await page.SaveAsync();
                } else {
                    // replace existing
                    //RESEARCH: do we really need this?
                }
                // unzip ALL files but replace guid if it's part of the path
                foreach (var file in serModule.Files) {
                    ZipEntry e = zip.GetEntry(YetaWFZipFile.CleanFileName(file.FileName));
                    if (HaveManager && file.SiteSpecific) {
                        string fName = file.FileName;
                        fName = fName.Replace(originalGuid.ToString(), modDef.ModuleGuid.ToString());
                        fName = Manager.SiteFolder + fName;
                        await FileSystem.FileSystemProvider.CreateDirectoryAsync(Path.GetDirectoryName(fName)!);
                        using (IFileStream fs = await FileSystem.FileSystemProvider.CreateFileStreamAsync(fName)) {
                            using (Stream entryStream = zip.GetInputStream(e)) {
                                Extract(entryStream, fs);
                            }
                            await fs.CloseAsync();
                        }

                        // open file and replace old module guid with new module guid (this is mostly in case module has guid embedded in data)
                        string contents = await FileSystem.FileSystemProvider.ReadAllTextAsync(fName);
                        string newContents = contents.Replace(originalGuid.ToString(), modDef.ModuleGuid.ToString());
                        if (contents != newContents)
                            await FileSystem.FileSystemProvider.WriteAllTextAsync(fName, newContents);
                    } else {
                        throw new Error(__ResStr("nonSite", "Module Data {0} cannot be imported - It contains unexpected non site specific data", serModule.ModuleName));
                    }
                }
            } catch (Exception exc) {
                errorList.Add(__ResStr("errCantImport", "Module Data {0}({1}) cannot be imported - {2}", serModule.ModuleName, serModule.ModuleVersion, ErrorHandling.FormatExceptionMessage(exc)));
                return false;
            }
            return true;
        }

        private static void Extract(Stream entryStream, IFileStream fs) {
            byte[] buffer = new byte[4096];
            StreamUtils.Copy(entryStream, fs.GetFileStream(), buffer);
        }
    }
}
