/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using YetaWF.Core.DataProvider;
using YetaWF.Core.PackageSupport;
using YetaWF.Core.Support;
using YetaWF.Core.Support.Serializers;
using YetaWF.Core.Upload;
using YetaWF.PackageAttributes;

namespace YetaWF.Core.Packages {

    public partial class Package {

        //private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Package), name, defaultValue, parms); }

        public static bool Import(string zipFileName, List<string> errorList) {

            string displayFileName = FileUpload.IsUploadedFile(zipFileName) ? __ResStr("uploadedFile", "Uploaded file") : Path.GetFileName(zipFileName);

            string xmlFile = null;

            using (ZipFile zip = ZipFile.Read(zipFileName)) {

                // check id file
                ZipEntry ze = zip[PackageIDFile];
                if (ze == null) {
                    errorList.Add(string.Format(__ResStr("invFormat", "{0} is not a valid binary or source code package file."), displayFileName));
                    return false;
                }

                // read contents file
                xmlFile = Path.GetTempFileName();
                FileStream fs = new FileStream(xmlFile, FileMode.Create, FileAccess.ReadWrite);
                ze = zip[PackageContentsFile];
                ze.Extract(fs);
                fs.Close();

                fs = new FileStream(xmlFile, FileMode.Open, FileAccess.Read);
                SerializablePackage serPackage = (SerializablePackage)new GeneralFormatter(Package.ExportFormat).Deserialize(fs);
                fs.Close();

                File.Delete(xmlFile);

                if (Package.CompareVersion(YetaWF.Core.Controllers.AreaRegistration.CurrentPackage.Version, serPackage.CoreVersion) < 0) {
                    errorList.Add(string.Format(__ResStr("invCore", "This package requires YetaWF version {0} - Current version found is {1}"), serPackage.CoreVersion, YetaWF.Core.Controllers.AreaRegistration.CurrentPackage.Version));
                    return false;
                }
                return Import(zip, displayFileName, serPackage, errorList);
            }
        }

        private static bool Import(ZipFile zip, string displayFileName, SerializablePackage serPackage, List<string> errorList) {

            // unzip all files/data
            try {
                // determine whether we have source code
                bool hasSource = (serPackage.SourceFiles != null && serPackage.SourceFiles.Count > 0);
                string sourcePath = null;
                if (hasSource) {
                    // Determine whether this is a YetaWF instance with source code by inspecting the Core package
                    if (!YetaWF.Core.Controllers.AreaRegistration.CurrentPackage.HasSource)
                        throw new InternalError("Packages with source code can only be imported on development systems");
                    // set target folder
                    string sourceFolder = null;
                    switch (serPackage.PackageType) {
                        case PackageTypeEnum.Module:
                            sourceFolder = WebConfigHelper.GetValue<string>(DataProviderImpl.DefaultString, "SourceFolder_Modules", "Modules");
                            break;
                        case PackageTypeEnum.Skin:
                            sourceFolder = WebConfigHelper.GetValue<string>(DataProviderImpl.DefaultString, "SourceFolder_Skins", "Skins");
                            break;
                        case PackageTypeEnum.Core:
                        case PackageTypeEnum.CoreAssembly:
                            sourceFolder = WebConfigHelper.GetValue<string>(DataProviderImpl.DefaultString, "SourceFolder_Core", "Core");
                            break;
                        case PackageTypeEnum.DataProvider:
                            sourceFolder = WebConfigHelper.GetValue<string>(DataProviderImpl.DefaultString, "SourceFolder_DataProvider", "DataProvider");
                            break;
                        case PackageTypeEnum.Template:
                            sourceFolder = WebConfigHelper.GetValue<string>(DataProviderImpl.DefaultString, "SourceFolder_Templates", "Templates");
                            break;
                        case PackageTypeEnum.Utility:
                            sourceFolder = WebConfigHelper.GetValue<string>(DataProviderImpl.DefaultString, "SourceFolder_Utilities", "Utilities");
                            break;
                        default:
                            errorList.Add(__ResStr("errPackageType", "Unsupported package type {0}", serPackage.PackageType));
                            return true;
                    }
                    sourcePath = Path.Combine(YetaWFManager.RootFolder, "..", sourceFolder, serPackage.PackageDomain, serPackage.PackageProduct);
                    try {
                        Directory.Delete(sourcePath, true);
                    } catch (Exception exc) {
                        if (!(exc is DirectoryNotFoundException)) {
                            errorList.Add(__ResStr("cantDelete", "Package source folder {0} could not be deleted: {1}", sourcePath, exc.Message));
                            return false;
                        }
                    }
                    Directory.CreateDirectory(sourcePath);
                }

                string addonsPath = Path.Combine(YetaWFManager.RootFolder, Globals.AddOnsFolder, serPackage.PackageDomain, serPackage.PackageProduct);
                try {
                    Directory.Delete(Path.Combine(addonsPath), true);
                } catch (Exception exc) {
                    if (!(exc is DirectoryNotFoundException)) {
                        errorList.Add(__ResStr("cantDeleteAddons", "Site Addons folder {0} could not be deleted: {1}", addonsPath, exc.Message));
                        return false;
                    }
                }
                string viewsPath = Path.Combine(YetaWFManager.RootFolder, Globals.AreasFolder, serPackage.PackageName.Replace(".", "_"), Globals.ViewsFolder);
                try {
                    Directory.Delete(Path.Combine(viewsPath), true);
                } catch (Exception exc) {
                    if (!(exc is DirectoryNotFoundException)) {
                        errorList.Add(__ResStr("cantDeleteViews", "Site Views folder {0} could not be deleted: {1}", viewsPath, exc.Message));
                        return false;
                    }
                }

                // bin files
                {
                    string tempBin = Path.Combine(YetaWFManager.RootFolder, "bin", "temp", serPackage.PackageDomain, serPackage.PackageProduct);
                    foreach (var file in serPackage.BinFiles) {
                        ZipEntry e = zip[file.FileName];
                        e.Extract(tempBin, ExtractExistingFileAction.OverwriteSilently);
                    }
                    CopyVersionedFiles(tempBin, YetaWFManager.RootFolder);
                    try {// try to delete all dirs up to bin/temp if empty (ignore any errors)
                        Directory.Delete(Path.Combine(YetaWFManager.RootFolder, "bin", "temp", serPackage.PackageDomain, serPackage.PackageProduct), true);
                        Directory.Delete(Path.Combine(YetaWFManager.RootFolder, "bin", "temp", serPackage.PackageDomain));
                        Directory.Delete(Path.Combine(YetaWFManager.RootFolder, "bin", "temp"));
                    } catch (Exception) { }
                }

                if (!hasSource) {
                    // Addons
                    foreach (var file in serPackage.AddOns) {
                        ZipEntry e = zip[file.FileName];
                        e.Extract(YetaWFManager.RootFolder, ExtractExistingFileAction.OverwriteSilently);
                    }
                    // Views
                    foreach (var file in serPackage.Views) {
                        ZipEntry e = zip[file.FileName];
                        e.Extract(YetaWFManager.RootFolder, ExtractExistingFileAction.OverwriteSilently);
                    }
                } else {
                    // bin
                    foreach (var file in serPackage.BinFiles) {
                        ZipEntry e = zip[file.FileName];
                        e.Extract(sourcePath, ExtractExistingFileAction.OverwriteSilently);
                    }
                    // Source code (optional), includes addons & views
                    foreach (var file in serPackage.SourceFiles) {
                        ZipEntry e = zip[file.FileName];
                        e.Extract(sourcePath, ExtractExistingFileAction.OverwriteSilently);
                    }

                    // Create symlink/junction from web to project addons
                    string from = addonsPath;
                    string to = Path.Combine(sourcePath, Globals.AddOnsFolder);
                    if (!CreatePackageSymLink(from, to))
                        errorList.Add(__ResStr("errSymlink", "Couldn't create symbolic link/junction from {0} to {1} - You will have to investigate the failure and manually create the link using:(+nl)mklink /D \"{0}\",\"{1}\"", from, to));
                    // Create symlink/junction from web views to project package views
                    from = viewsPath;
                    to = Path.Combine(sourcePath, Globals.ViewsFolder);
                    if (!CreatePackageSymLink(from, to))
                        errorList.Add(__ResStr("errSymlink", "Couldn't create symbolic link/junction from {0} to {1} - You will have to investigate the failure and manually create the link using:(+nl)mklink /D \"{0}\",\"{1}\"", from, to));

                    errorList.Add(__ResStr("addProject", "You now have to add the project to your Visual Studio solution and add a project reference to the YetaWF site (Website) so it is built correctly. Without this reference the site will not use the new package when it's rebuilt using Visual Studio."));
                }
            } catch (Exception exc) {
                errorList.Add(string.Format(__ResStr("errCantImport", "Package {0}({1}) cannot be imported - {2}"), serPackage.PackageName, serPackage.PackageVersion, exc.Message));
                return false;
            }
            return true;
        }
        private static void CopyVersionedFiles(string sourceFolder, string targetFolder) {
            string[] files = Directory.GetFiles(sourceFolder);
            foreach (string file in files)
                CopyVersionedFile(file, targetFolder);
            string[] dirs = Directory.GetDirectories(sourceFolder);
            foreach (string dir in dirs)
                CopyVersionedFiles(dir, Path.Combine(targetFolder, Path.GetFileName(dir)));
        }
        private static void CopyVersionedFile(string sourceFile, string targetFolder) {
            string targetFile = Path.Combine(targetFolder, Path.GetFileName(sourceFile));
            if (File.Exists(targetFile)) {
                FileVersionInfo versTarget = FileVersionInfo.GetVersionInfo(targetFile);
                FileVersionInfo versSource = FileVersionInfo.GetVersionInfo(sourceFile);
                if (!string.IsNullOrWhiteSpace(versTarget.FileVersion) && !string.IsNullOrWhiteSpace(versSource.FileVersion)) {
                    if (Package.CompareVersion(versTarget.FileVersion, versSource.FileVersion) >= 0)
                        return; // no need to copy, target version > source version
                }
            }
            DateTime modTarget = File.GetLastWriteTimeUtc(targetFile);
            DateTime modSource = File.GetLastWriteTimeUtc(sourceFile);
            if (modTarget >= modSource)
                return; // no need to copy, target modified date/time > source modified
            File.Copy(sourceFile, targetFile, true);
        }
        public static bool CreatePackageSymLink(string from, string to) {
            //RESEARCH:  SE_CREATE_SYMBOLIC_LINK_NAME
            // secpol.msc
            // Local Policies > User Rights Assignments - Create Symbolic Links
            // IIS APPPOOL\application-pool
            // Needs special UAC/elevation so it's not usable for our purposes
            // return CreateSymbolicLink(from, to, (int)SymbolicLink.Directory) != 0;
            // use junctions instead (no special privilege needed)
            Junction.Create(from, to, true);
            return true;
        }

        //enum SymbolicLink {
        //    File = 0,
        //    Directory = 1
        //}
        //[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass")]
        //[DllImport("kernel32.dll", EntryPoint = "CreateSymbolicLinkW", CharSet = CharSet.Unicode)]
        //internal static extern int CreateSymbolicLink([In] string lpSymlinkFileName, [In] string lpTargetFileName, int dwFlags);
    }
}
