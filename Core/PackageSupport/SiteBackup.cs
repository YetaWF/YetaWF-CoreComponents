/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YetaWF.Core.IO;
using YetaWF.Core.Localize;
using YetaWF.Core.Serializers;
using YetaWF.Core.Support;
using YetaWF.Core.Support.Serializers;

namespace YetaWF.Core.Packages {

    public class SiteBackup {

        protected static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        public const string SiteIDFile = "Site.txt";
        public const string SiteIDDataFile = "SiteData.txt";
        public const string SiteContentsFile = "Contents.xml";
        public const string BackupFolder = "Backups";
        public const string BackupFileFormat = "Backup {0}";
        public const string BackupDateTimeFormat = "yyyy-MM-dd HH-mm-ss";

        public class SerializableSiteBackup {
            public string PackageName { get; set; }
            public string PackageVersion { get; set; }
            public DateTime Created { get; set; }
            public SerializableList<string> PackageDataFiles { get; set; }
            public SerializableList<string> CustomAddonFiles { get; set; }
            public SerializableList<string> PackageFiles { get; set; }
            public SerializableSiteBackup() {
                PackageDataFiles = new SerializableList<string>();
                CustomAddonFiles = new SerializableList<string>();
                PackageFiles = new SerializableList<string>();
            }
        }

        public async Task<bool> CreateAsync(List<string> errorList, bool ForDistribution = false, bool DataOnly = false) {

            SerializableSiteBackup serBackup;
            using (YetaWFZipFile zipBackupFile = MakeZipFile(out serBackup)) {

                // create backup file in backup folder
                string backupFolder = Path.Combine(Manager.SiteFolder, BackupFolder);
                await FileSystem.FileSystemProvider.CreateDirectoryAsync(backupFolder);
                // create a don't deploy marker
                await FileSystem.FileSystemProvider.WriteAllTextAsync(Path.Combine(backupFolder, Globals.DontDeployMarker), "");

                string tempFolder = Path.Combine(backupFolder, Path.GetRandomFileName());
                await FileSystem.TempFileSystemProvider.CreateDirectoryAsync(tempFolder);
                zipBackupFile.TempFolders.Add(tempFolder);

                // backup data for each package
                List<Package> packages = (from p in Package.GetAvailablePackages() select p).ToList();// copy
                //packages.AddRange(Package.GetUtilityPackages());
                foreach (Package package in packages) {
                    YetaWFZipFile zipFile;
                    string file;
                    if (package.IsModulePackage || package.IsCorePackage) {
                        using (zipFile = await package.ExportDataAsync(takingBackup: true)) {
                            file = Path.Combine(tempFolder, zipFile.Zip.Name);
                            zipFile.Zip.Save(file);
                            serBackup.PackageDataFiles.Add(zipFile.Zip.Name);
                        }
                    }
                    if (ForDistribution && await package.GetHasSourceAsync() && !DataOnly) {
                        using (zipFile = await package.ExportPackageAsync(SourceCode: true)) {
                            file = Path.Combine(tempFolder, zipFile.Zip.Name);
                            zipFile.Zip.Save(file);
                            serBackup.PackageFiles.Add(zipFile.Zip.Name);
                        }
                        using (zipFile = await package.ExportPackageAsync(SourceCode: false)) {
                            file = Path.Combine(tempFolder, zipFile.Zip.Name);
                            zipFile.Zip.Save(file);
                            serBackup.PackageFiles.Add(zipFile.Zip.Name);
                        }
                    } else {
                        using (zipFile = await package.ExportPackageAsync(SourceCode: await package.GetHasSourceAsync())) {
                            file = Path.Combine(tempFolder, zipFile.Zip.Name);
                            zipFile.Zip.Save(file);
                            serBackup.PackageFiles.Add(zipFile.Zip.Name);
                        }
                    }
                }

                // backup custom addons (site specific)
                SerializableList<SerializableFile> fileList = await Package.ProcessAllFilesAsync(Manager.AddonsCustomSiteFolder, Package.ExcludedFilesAddons);
                foreach (var file in fileList) {
                    ZipEntry ze = zipBackupFile.Zip.AddFile(file.AbsFileName);
                    ze.FileName = file.FileName;
                    serBackup.CustomAddonFiles.Add(file.AbsFileName);
                }

                // serialize backup file
                {
                    string fileName = Path.GetTempFileName();
                    zipBackupFile.TempFiles.Add(fileName);

                    using (IFileStream fs = await FileSystem.TempFileSystemProvider.CreateFileStreamAsync(fileName)) {
                        new GeneralFormatter(Package.ExportFormat).Serialize(fs.GetFileStream(), serBackup);
                        await fs.CloseAsync();
                    }

                    ZipEntry ze = zipBackupFile.Zip.AddFile(fileName);
                    ze.FileName = SiteContentsFile;

                    zipBackupFile.Zip.AddDirectory(tempFolder);
                    zipBackupFile.Zip.AddEntry(SiteIDFile, this.__ResStr("backup", "YetaWF Site Backup"));
                    zipBackupFile.Zip.Save(Path.Combine(backupFolder, string.Format(BackupFileFormat + ".zip", serBackup.Created.ToString(BackupDateTimeFormat))));
                }
            }
            return true;
        }
        private YetaWFZipFile MakeZipFile(out SerializableSiteBackup serBackup) {
            Package executingPackage = Package.GetCurrentPackage(this);
            serBackup = new SerializableSiteBackup();
            serBackup.PackageName = executingPackage.Name;
            serBackup.PackageVersion = executingPackage.Version;
            serBackup.Created = DateTime.UtcNow;

            return new YetaWFZipFile {
                Zip = new ZipFile(),
            };
        }
        public async Task RemoveAsync(string filename) {
            filename = Path.ChangeExtension(filename, "zip");
            string path = Path.Combine(Manager.SiteFolder, SiteBackup.BackupFolder, filename);
            if (!await FileSystem.FileSystemProvider.FileExistsAsync(path))
                throw new Error(this.__ResStr("backupNotFound", "The backup '{0}' cannot be located.", filename));
            await FileSystem.FileSystemProvider.DeleteFileAsync(path);
        }

        public async Task RemoveOldBackupsAsync(List<string> errorList, int maxDays) {
            string backupFolder = Path.Combine(Manager.SiteFolder, BackupFolder);
            if (!await FileSystem.FileSystemProvider.DirectoryExistsAsync(backupFolder)) return;
            List<string> backups = await FileSystem.FileSystemProvider.GetFilesAsync(backupFolder);
            TimeSpan timeSpan = new TimeSpan(maxDays, 0, 0, 0);
            foreach (string backup in backups) {
                if (await FileSystem.FileSystemProvider.GetLastWriteTimeUtcAsync(backup) < DateTime.UtcNow.Subtract(timeSpan))
                    await FileSystem.FileSystemProvider.DeleteFileAsync(backup);
            }
        }
    }
}
