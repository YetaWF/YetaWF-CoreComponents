/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using Ionic.Zip;
using System;
using System.IO;
using System.Text.RegularExpressions;
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
        public static string[] ExcludedFoldersNoSource = new string[] { "_License" };
        public static string[] ExcludedFilesSource = new string[] { ".csproj.user", ".pdb", ".xproj.user", ".lock.json" };
        public static string[] ExcludedFoldersSource = new string[] { "obj", "bin", "_License" };
        public static string[] ExcludedBinFiles = new string[] { ".config", ".pdb", ".lastcodeanalysissucceeded", ".CodeAnalysisLog.xml" };
        public static string[] ExcludedBinFolders = new string[] { "Debug" };
        public static string[] ExcludedFilesViewsNoSource = new string[] { ".cs" };// not necessary any longer (since 2.0.0) as all code was moved to ViewsCode

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
            serPackage.BinFiles.AddRange(ProcessAllFiles(sourceBin, ExcludedBinFiles, ExcludedBinFolders, ExternalRoot: PackageSourceRoot));
            foreach (var file in serPackage.BinFiles) {
                ZipEntry ze = zipFile.Zip.AddFile(file.AbsFileName);
                ze.FileName = file.FileName;
            }
            if (!SourceCode) {
                // Addons
                if (PackageType == PackageTypeEnum.Module || PackageType == PackageTypeEnum.Skin) {
                    serPackage.AddOns.AddRange(ProcessAllFiles(AddonsFolder, ExcludedFilesAddons, ExcludedFoldersNoSource));
                    foreach (var file in serPackage.AddOns) {
                        ZipEntry ze = zipFile.Zip.AddFile(file.AbsFileName);
                        ze.FileName = file.FileName;
                    }
                }
                // Views
                string rootFolder;
#if MVC6
                rootFolder = YetaWFManager.RootFolderSolution;
#else
                rootFolder = YetaWFManager.RootFolder;
#endif
                string viewsPath = Path.Combine(rootFolder, Globals.AreasFolder, serPackage.PackageName.Replace(".", "_"), Globals.ViewsFolder);
                serPackage.Views.AddRange(ProcessAllFiles(viewsPath, ExcludedFilesViewsNoSource));
                foreach (var file in serPackage.Views) {
                    ZipEntry ze = zipFile.Zip.AddFile(file.AbsFileName);
                    ze.FileName = file.FileName;
                }
            }
            // Source code
            if (SourceCode) {
                serPackage.SourceFiles.AddRange(ProcessAllFiles(PackageSourceRoot, ExcludedFilesSource, ExcludedFoldersSource, ExternalRoot: PackageSourceRoot));
                ProcessSourceFiles(zipFile, serPackage.SourceFiles);
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
            serPackage.AspNetMvcVersion = this.AspNetMvc;

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
        private static void AddFiles(SerializableList<SerializableFile> list, string folder, string[] excludeFiles = null, string[] excludeFolders = null, string ExternalRoot = null) {
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
        /// <summary>
        /// Read all *.cs files and remove #if LICENSED ... #endif.
        /// Read all *.csproj files and remove <DefineConstants> ... LICENSED ... </DefineConstants> and <ProjectReference></ProjectReference>
        /// </summary>
        /// <param name="zipFile">The ZIP file.</param>
        /// <param name="sourceFiles">The full file name to process.</param>
        /// <remarks>Used to remove license validation code when distributing source code packages.</remarks>
        private void ProcessSourceFiles(YetaWFZipFile zipFile, SerializableList<SerializableFile> sourceFiles) {
            foreach (var sourceFile in sourceFiles) {
                string text = File.ReadAllText(sourceFile.AbsFileName);
                string newText = ProcessCs(sourceFile, text);
                newText = ProcessCsProj(sourceFile, newText);
                // if there were changes, replace the real file with a temp file/new contents
                if (text != newText) {
                    string tempFile = Path.GetTempFileName();
                    File.Delete(tempFile);
                    File.WriteAllText(tempFile, newText);
                    sourceFile.ReplaceAbsFileName(tempFile);
                    zipFile.TempFiles.Add(tempFile);
                }
            }
        }
        private Regex reIfLicensed = new Regex(@"(\n|\r)\s*#\s*if\s+LICENSED.*?(\n|\r).*?\s*#\s*endif.*?(\n|\r)", RegexOptions.Singleline | RegexOptions.Compiled);

        private string ProcessCs(SerializableFile sourceFile, string text) {
            return reIfLicensed.Replace(text, "$1$2");
        }

        private Regex reCsProjDefCon1 = new Regex(@"\<DefineConstants\>(.*?);LICENSED\<\/DefineConstants\>", RegexOptions.Singleline | RegexOptions.Compiled);
        private Regex reCsProjDefCon2 = new Regex(@"\<DefineConstants\>(.*?)LICENSED;(.*?)\<\/DefineConstants\>", RegexOptions.Singleline | RegexOptions.Compiled);
        private Regex reCsProjRef = new Regex(@"\<ProjectReference\s+[^\>]*\>\s*\<Project\>[^\<]*\</Project\>\s*\<Name\>PackageVerificationAssembly\<\/Name\>\s*\<\/ProjectReference\>", RegexOptions.Singleline | RegexOptions.Compiled);

        private string ProcessCsProj(SerializableFile sourceFile, string text) {
            text = reCsProjDefCon1.Replace(text, "<DefineConstants>$1</DefineConstants>");
            text = reCsProjDefCon2.Replace(text, "<DefineConstants>$1$2</DefineConstants>");
            return reCsProjRef.Replace(text, "");
        }
    }
}
