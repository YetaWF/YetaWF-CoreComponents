/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using Ionic.Zip;
using System;
using System.IO;
using YetaWF.Core.Serializers;
using YetaWF.Core.Support;
using YetaWF.Core.Support.Serializers;
using YetaWF.PackageAttributes;

namespace YetaWF.Core.Packages {

    public partial class Package {

        //private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Package), name, defaultValue, parms); }

        public const string PackageIDFile = "Package.txt";
        public const string PackageIDDataFile = "PackageData.txt";
        public const string PackageContentsFile = "Contents.xml";
        public static string[] ExcludedFilesAddons = new string[] { };
        public static string[] ExcludedFoldersAddons = new string[] { "_License" };
        public static string[] ExcludedFilesSource = new string[] { ".csproj.user", ".pdb", };
        public static string[] ExcludedBinFiles = new string[] { ".config", ".pdb" };

        public const GeneralFormatter.Style ExportFormat = GeneralFormatter.Style.Xml;

        public YetaWFZipFile ExportPackage(bool SourceCode = false) {

            if (SourceCode && !HasSource)
                throw new InternalError("Package export requested for package {0} which is not exportable (not a source package)", Name);

            string zipName = SourceCode ?
                    string.Format(__ResStr("packageFmtSrc", "Package (w_Source) - {0}.{1}.zip", this.Name, this.Version)) :
                    string.Format(__ResStr("packageFmt", "Package - {0}.{1}.zip", this.Name, this.Version));

            SerializablePackage serPackage;
            YetaWFZipFile zipFile = MakeZipFile(zipName, out serPackage);
            zipFile.HasSource = SourceCode;
            serPackage.PackageDomain = Domain;
            serPackage.PackageProduct = Product;
            serPackage.PackageType = PackageType;

            // all bin files
            string sourceBin = Path.Combine(PackageSourceRoot, "bin");
            serPackage.BinFiles.AddRange(ProcessAllFiles(sourceBin, ExcludedBinFiles, null, ExternalRoot: PackageSourceRoot));
            foreach (var file in serPackage.BinFiles) {
                ZipEntry ze = zipFile.Zip.AddFile(file.AbsFileName);
                ze.FileName = file.FileName;
            }
            if (!SourceCode) {
                // Addons
                if (PackageType == PackageTypeEnum.Module || PackageType == PackageTypeEnum.Skin) {
                    serPackage.AddOns.AddRange(ProcessAllFiles(AddonsFolder, ExcludedFilesAddons, ExcludedFoldersAddons));
                    foreach (var file in serPackage.AddOns) {
                        ZipEntry ze = zipFile.Zip.AddFile(file.AbsFileName);
                        ze.FileName = file.FileName;
                    }
                }
                // Views
                string viewsPath = Path.Combine(YetaWFManager.RootFolder, Globals.AreasFolder, serPackage.PackageName.Replace(".", "_"), Globals.ViewsFolder);
                serPackage.Views.AddRange(ProcessAllFiles(viewsPath));
                foreach (var file in serPackage.Views) {
                    ZipEntry ze = zipFile.Zip.AddFile(file.AbsFileName);
                    ze.FileName = file.FileName;
                }
            }
            // Source code
            if (SourceCode) {
                string[] excludedFoldersSource = new string[] { "obj", "bin" };
                serPackage.SourceFiles.AddRange(ProcessAllFiles(PackageSourceRoot, ExcludedFilesSource, excludedFoldersSource, ExternalRoot: PackageSourceRoot));
                foreach (var file in serPackage.SourceFiles) {
                    ZipEntry ze = zipFile.Zip.AddFile(file.AbsFileName);
                    ze.FileName = file.FileName;
                }
            }

            // serialize package contents
            {
                string fileName = Path.GetTempFileName();
                zipFile.TempFiles.Add(fileName);

                FileStream fs = new FileStream(fileName, FileMode.Create);
                new GeneralFormatter(Package.ExportFormat).Serialize(fs, serPackage);
                fs.Close();

                ZipEntry ze = zipFile.Zip.AddFile(fileName);
                ze.FileName = PackageContentsFile;
            }
            zipFile.Zip.AddEntry(PackageIDFile, __ResStr("package", "YetaWF Package"));

            return zipFile;
        }

        private YetaWFZipFile MakeZipFile(string zipName, out SerializablePackage serPackage) {
            serPackage = new SerializablePackage();
            serPackage.PackageName = this.Name;
            serPackage.PackageVersion = this.Version;
            serPackage.CoreVersion = YetaWF.Core.Controllers.AreaRegistration.CurrentPackage.Version;

            return new YetaWFZipFile {
                FileName = zipName,
                Zip = new ZipFile(zipName),
            };
        }
        public static SerializableList<SerializableFile> ProcessAllFiles(string folder, string[] excludeFiles = null, string[] excludeFolders = null, string ExternalRoot = null) {
            SerializableList<SerializableFile> list = new SerializableList<SerializableFile>();
            AddFiles(list, folder, excludeFiles, excludeFolders, ExternalRoot: ExternalRoot);
            return list;
        }
        public static void AddFiles(SerializableList<SerializableFile> list, string folder, string[] excludeFiles = null, string[] excludeFolders = null, string ExternalRoot = null) {
            if (!Directory.Exists(folder))
                return;
            foreach (var file in Directory.GetFiles(folder)) {
                bool copy = true;
                if (excludeFiles != null) {
                    foreach (var x in excludeFiles) {
                        if (file.EndsWith(x, StringComparison.InvariantCultureIgnoreCase)) {
                            copy = false;
                            break;
                        }
                    }
                }
                if (copy)
                    list.Add(new SerializableFile(file, ExternalRoot: ExternalRoot));
            }
            foreach (var dir in Directory.GetDirectories(folder)) {
                string dirName = Path.GetFileName(dir).ToLower();
                bool copy = true;
                if (excludeFolders != null) {
                    foreach (var x in excludeFolders) {
                        if (dirName == x.ToLower()) {
                            copy = false;
                            break;
                        }
                    }
                }
                if (copy)
                    AddFiles(list, dir, excludeFiles, excludeFolders, ExternalRoot: ExternalRoot);
            }
        }
    }
}
