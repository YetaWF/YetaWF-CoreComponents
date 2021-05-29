/* Copyright © 2021 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YetaWF.Core.IO;
using YetaWF.Core.Language;
using YetaWF.Core.Models;
using YetaWF.Core.Serializers;
using YetaWF.Core.Support;
using YetaWF.Core.Support.Serializers;
using YetaWF.Core.Support.Zip;
using YetaWF.PackageAttributes;

namespace YetaWF.Core.Packages {

    public partial class Package {

        /* private static string __ResStr(string name, string defaultValue, params object?[] parms) { return ResourceAccess.GetResourceString(typeof(Package), name, defaultValue, parms); } */

        public const string PackageIDFile = "Package.txt";
        public const string PackageIDDataFile = "PackageData.txt";
        public const string PackageContentsFile = "Contents.json";
        public static string[] ExcludedFilesAddons = Array.Empty<string>();
        public static string[] ExcludedFoldersNoSource = Array.Empty<string>();
        public static string[] ExcludedFilesSource = new string[] { ".csproj.user", ".pdb", ".xproj.user", ".lock.json" };
        public static string[] ExcludedFoldersSource = new string[] { "obj", "bin", Globals.NodeModulesFolder };
        public static string[] ExcludedBinFiles = new string[] { ".config", ".pdb", ".lastcodeanalysissucceeded", ".CodeAnalysisLog.xml" };
        public static string[] ExcludedBinFolders = new string[] { "Debug" };
        public static string[] ExcludedFilesViewsNoSource = new string[] { ".cs" };// not necessary any longer (since 2.0.0) as all code was moved to ViewsCode

        public const GeneralFormatter.Style ExportFormat = GeneralFormatter.Style.JSON;
        public const GeneralFormatter.Style ExportFormatModules = GeneralFormatter.Style.JSONTyped;
        public const GeneralFormatter.Style ExportFormatChunks = GeneralFormatter.Style.JSONTyped;

        public async Task<YetaWFZipFile> ExportPackageAsync(bool SourceCode = false) {

            if (SourceCode && !await GetHasSourceAsync())
                throw new InternalError("Package export requested for package {0} which is not exportable (not a source package)", Name);

            string zipName = SourceCode ?
                    __ResStr("packageFmtSrcCore", "Package w_Source ASPNETCore - {0}.{1}.zip", this.Name, this.Version)
                :
                    __ResStr("packageFmtCore", "Package ASPNETCore - {0}.{1}.zip", this.Name, this.Version)
                ;

            SerializablePackage serPackage;
            YetaWFZipFile zipFile = MakeZipFile(zipName, out serPackage);
            serPackage.PackageDomain = Domain;
            serPackage.PackageProduct = Product;
            serPackage.PackageType = PackageType;

            // all bin files
            string sourceBin = Path.Combine(PackageSourceRoot, "bin");
            serPackage.BinFiles.AddRange(await ProcessAllFilesAsync(sourceBin, ExcludedBinFiles, ExcludedBinFolders, ExternalRoot: PackageSourceRoot));
            foreach (var file in serPackage.BinFiles) {
                zipFile.AddFile(file.AbsFileName, file.FileName);
            }
            if (!SourceCode) {
                // Addons
                if (PackageType == PackageTypeEnum.Module || PackageType == PackageTypeEnum.Skin) {
                    serPackage.AddOns.AddRange(await ProcessAllFilesAsync(AddonsSourceFolder, ExcludedFilesAddons, ExcludedFoldersNoSource, ExternalRoot: YetaWFManager.RootFolder));
                    foreach (var file in serPackage.AddOns) {
                        zipFile.AddFile(file.AbsFileName, file.FileName);
                    }
                }
            }
            // Source code
            if (SourceCode) {
                // package source
                serPackage.SourceFiles.AddRange(await ProcessAllFilesAsync(PackageSourceRoot, ExcludedFilesSource, ExcludedFoldersSource, ExternalRoot: PackageSourceRoot));
                await ProcessSourceFilesAsync(zipFile, serPackage.SourceFiles);
                foreach (var file in serPackage.SourceFiles) {
                    zipFile.AddFile(file.AbsFileName, file.FileName);
                }
            }
            // localization
            foreach (LanguageData languageData in MultiString.Languages) {
                List<string> files = await Localization.GetFilesAsync(this, languageData.Id, true);
                foreach (string file in files) {
                    SerializableFile serFile = new SerializableFile(file, ExternalRoot: YetaWFManager.RootFolderWebProject);
                    serFile.FileDate = await FileSystem.FileSystemProvider.GetCreationTimeUtcAsync(serFile.AbsFileName);
                    serPackage.LocalizationFiles.Add(serFile);
                }
            }
            foreach (SerializableFile file in serPackage.LocalizationFiles) {
                zipFile.AddFile(file.AbsFileName, file.FileName);
            }

            // serialize package contents
            {
                string fileName = FileSystem.TempFileSystemProvider.GetTempFile();
                zipFile.TempFiles.Add(fileName);

                using (IFileStream fs = await FileSystem.TempFileSystemProvider.CreateFileStreamAsync(fileName)) {
                    new GeneralFormatter(Package.ExportFormat).Serialize(fs.GetFileStream(), serPackage);
                    await fs.CloseAsync();
                }

                zipFile.AddFile(fileName, PackageContentsFile);
            }
            zipFile.AddData("YetaWF Package", PackageIDFile);

            return zipFile;
        }

        private YetaWFZipFile MakeZipFile(string zipName, out SerializablePackage serPackage) {
            serPackage = new SerializablePackage();
            serPackage.PackageName = this.Name;
            serPackage.PackageVersion = this.Version;
            serPackage.CoreVersion = YetaWF.Core.AreaRegistration.CurrentPackage.Version;
            serPackage.AspNetMvcVersion = this.AspNetMvc;

            return new YetaWFZipFile {
                FileName = zipName,
            };
        }
        public static async Task<SerializableList<SerializableFile>> ProcessAllFilesAsync(string folder, string[]? excludeFiles = null, string[]? excludeFolders = null, string? ExternalRoot = null) {
            SerializableList<SerializableFile> list = new SerializableList<SerializableFile>();
            await AddFilesAsync(list, folder, excludeFiles, excludeFolders, ExternalRoot: ExternalRoot);
            return list;
        }
        private static async Task AddFilesAsync(SerializableList<SerializableFile> list, string folder, string[]? excludeFiles = null, string[]? excludeFolders = null, string? ExternalRoot = null) {
            if (!await FileSystem.FileSystemProvider.DirectoryExistsAsync(folder))
                return;
            foreach (var file in await FileSystem.FileSystemProvider.GetFilesAsync(folder)) {
                bool copy = true;
                if (excludeFiles != null) {
                    foreach (var x in excludeFiles) {
                        if (file.EndsWith(x, StringComparison.InvariantCultureIgnoreCase)) {
                            copy = false;
                            break;
                        }
                    }
                }
                if (copy) {
                    SerializableFile serFile = new SerializableFile(file, ExternalRoot: ExternalRoot);
                    serFile.FileDate = await FileSystem.FileSystemProvider.GetCreationTimeUtcAsync(serFile.AbsFileName);
                    list.Add(serFile);
                }
            }
            foreach (var dir in await FileSystem.FileSystemProvider.GetDirectoriesAsync(folder)) {
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
                    await AddFilesAsync(list, dir, excludeFiles, excludeFolders, ExternalRoot: ExternalRoot);
            }
        }
        /// <summary>
        /// Read all *.cs files and remove #if LICENSED ... #endif.
        /// Read all *.csproj files and remove <DefineConstants> ... LICENSED ... </DefineConstants> and <ProjectReference></ProjectReference>
        /// </summary>
        /// <param name="zipFile">The ZIP file.</param>
        /// <param name="sourceFiles">The full file name to process.</param>
        /// <remarks>Used to remove license validation code when distributing source code packages.</remarks>
        private async Task ProcessSourceFilesAsync(YetaWFZipFile zipFile, SerializableList<SerializableFile> sourceFiles) {
            foreach (var sourceFile in sourceFiles) {
                string text = await FileSystem.FileSystemProvider.ReadAllTextAsync(sourceFile.AbsFileName);
                string newText = ProcessCs(sourceFile, text);
                newText = ProcessCsProj(sourceFile, newText);
                // if there were changes, replace the real file with a temp file/new contents
                if (text != newText) {
                    string tempFile = FileSystem.TempFileSystemProvider.GetTempFile();
                    zipFile.TempFiles.Add(tempFile);
                    await FileSystem.TempFileSystemProvider.DeleteFileAsync(tempFile);
                    await FileSystem.TempFileSystemProvider.WriteAllTextAsync(tempFile, newText);
                    sourceFile.ReplaceAbsFileName(tempFile);
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
