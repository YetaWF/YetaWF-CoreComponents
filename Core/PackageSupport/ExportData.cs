/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YetaWF.Core.DataProvider;
using YetaWF.Core.Identity;
using YetaWF.Core.IO;
using YetaWF.Core.Modules;
using YetaWF.Core.Pages;
using YetaWF.Core.Serializers;
using YetaWF.Core.Support;
using YetaWF.Core.Support.Serializers;
using YetaWF.Core.Support.Zip;

namespace YetaWF.Core.Packages {

    public partial class Package {

        /* private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Package), name, defaultValue, parms); } */

        public async Task<YetaWFZipFile> ExportDataAsync(bool takingBackup = false) {

            if (!IsModulePackage && !IsCorePackage)
                throw new InternalError("This package type has no associated data to export");

            string zipName = __ResStr("packageDataFmt", "Package Data - {0}.{1}.zip", this.Name, this.Version);

            SerializableData serData;
            string fileName;
            YetaWFZipFile zipFile = MakeZipFile(zipName, out serData);

            serData.Data = new SerializableList<SerializableModelData>();

            // add roles (we need to save role names in case we restore on another site with different role Ids)
            serData.Roles = new Serializers.SerializableList<RoleLookupEntry>(
                (from r in Resource.ResourceAccess.GetDefaultRoleList() select new RoleLookupEntry {
                    RoleId = r.RoleId,
                    RoleName = r.Name,
                }));

            // Export all models with data this package implements
            foreach (var modelType in this.InstallableModels) {
                try {
                    object instMod = Activator.CreateInstance(modelType);
                    using ((IDisposable)instMod) {

                        IInstallableModel model = (IInstallableModel)instMod;
                        if (!takingBackup || await model.IsInstalledAsync()) {

                            SerializableModelData serModel = new SerializableModelData();

                            bool more = true;
                            int chunk = 0;
                            for (; more; ++chunk) {
                                SerializableList<SerializableFile> fileList = new SerializableList<SerializableFile>();
                                DataProviderExportChunk expChunk = await model.ExportChunkAsync(chunk, fileList);
                                more = expChunk.More;

                                if (fileList != null && fileList.Count > 0) {
                                    serModel.Files.AddRange(fileList);
                                    foreach (var file in fileList) {
                                        zipFile.AddFile(file.AbsFileName, file.FileName);
                                    }
                                }
                                if (!more && expChunk.ObjectList == null)
                                    break;

                                // add users (we need to save user names in case we restore on another site with different user Ids)
                                IEnumerable ienumerable = expChunk.ObjectList as IEnumerable;
                                if (ienumerable != null) {
                                    IEnumerator ienum = ienumerable.GetEnumerator();
                                    while (ienum.MoveNext()) {
                                        PageDefinition pageDef = ienum.Current as PageDefinition;
                                        if (pageDef != null) {
                                            foreach (PageDefinition.AllowedUser user in pageDef.AllowedUsers) {
                                                if ((from u in serData.Users where u.UserId == user.UserId select u).FirstOrDefault() == null) {
                                                    serData.Users.Add(new UserLookupEntry {
                                                        UserId = user.UserId,
                                                        UserName = await Resource.ResourceAccess.GetUserNameAsync(user.UserId),
                                                    });
                                                }
                                            }
                                        }
                                        ModuleDefinition modDef = ienum.Current as ModuleDefinition;
                                        if (modDef != null) {
                                            foreach (ModuleDefinition.AllowedUser user in modDef.AllowedUsers) {
                                                if ((from u in serData.Users where u.UserId == user.UserId select u).FirstOrDefault() == null) {
                                                    serData.Users.Add(new UserLookupEntry {
                                                        UserId = user.UserId,
                                                        UserName = await Resource.ResourceAccess.GetUserNameAsync(user.UserId),
                                                    });
                                                }
                                            }
                                        }
                                    }
                                }

                                fileName = FileSystem.TempFileSystemProvider.GetTempFile();
                                zipFile.TempFiles.Add(fileName);

                                using (IFileStream fs = await FileSystem.TempFileSystemProvider.CreateFileStreamAsync(fileName)) {
                                    new GeneralFormatter(Package.ExportFormatChunks).Serialize(fs.GetFileStream(), expChunk.ObjectList);
                                    await fs.CloseAsync();
                                }

                                zipFile.AddFile(fileName, string.Format("{0}_{1}.json", modelType.Name, chunk));
                            }

                            serModel.Class = modelType.Name;
                            serModel.Chunks = chunk;

                            serData.Data.Add(serModel);
                        }
                    }
                } catch (Exception exc) {
                    throw new Error(__ResStr("errModCantExport", "Model type {0} cannot be exported - {1}"), modelType.FullName, ErrorHandling.FormatExceptionMessage(exc));
                }
            }

            // serialize package contents
            fileName = FileSystem.TempFileSystemProvider.GetTempFile();
            zipFile.TempFiles.Add(fileName);
            using (IFileStream fs = await FileSystem.TempFileSystemProvider.CreateFileStreamAsync(fileName)) {
                new GeneralFormatter(Package.ExportFormat).Serialize(fs.GetFileStream(), serData);
                await fs.CloseAsync();
            }
            zipFile.AddFile(fileName, PackageContentsFile);

            zipFile.AddData("YetaWF Package Data", PackageIDDataFile);

            return zipFile;
        }

        private YetaWFZipFile MakeZipFile(string zipName, out SerializableData serData) {
            serData = new SerializableData();
            serData.PackageName = this.Name;
            serData.PackageVersion = this.Version;

            return new YetaWFZipFile {
                FileName = zipName,
            };
        }
    }
}
