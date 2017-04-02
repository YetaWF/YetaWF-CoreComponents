/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using YetaWF.Core.Controllers;
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

                if (serPackage.AspNetMvcVersion != YetaWFManager.AspNetMvc) {
                    errorList.Add(string.Format(__ResStr("invMvc", "This package was built for {0}, but this site is running {1}",
                        YetaWFManager.GetAspNetMvcName(serPackage.AspNetMvcVersion), YetaWFManager.GetAspNetMvcName(YetaWFManager.AspNetMvc))));
                    return false;
                }
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
                    sourcePath = Path.Combine(YetaWFManager.RootFolderSolution, sourceFolder, serPackage.PackageDomain, serPackage.PackageProduct);
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
                string viewsPath;
#if MVC6
                viewsPath = Path.Combine(YetaWFManager.RootFolderWebProject, Globals.AreasFolder, serPackage.PackageName.Replace(".", "_"), Globals.ViewsFolder);
#else
                viewsPath = Path.Combine(YetaWFManager.RootFolder, Globals.AreasFolder, serPackage.PackageName.Replace(".", "_"), Globals.ViewsFolder);
#endif
                try {
                    Directory.Delete(Path.Combine(viewsPath), true);
                } catch (Exception exc) {
                    if (!(exc is DirectoryNotFoundException)) {
                        errorList.Add(__ResStr("cantDeleteViews", "Site Views folder {0} could not be deleted: {1}", viewsPath, exc.Message));
                        return false;
                    }
                }

                // copy bin files to website
                {
                    // copy bin files to a temporary location
                    string tempBin = Path.Combine(YetaWFManager.RootFolderWebProject, "tempbin", serPackage.PackageDomain, serPackage.PackageProduct);
                    foreach (var file in serPackage.BinFiles) {
                        ZipEntry e = zip[file.FileName];
                        e.Extract(tempBin, ExtractExistingFileAction.OverwriteSilently);
                    }
                    // copy bin files to required location
#if MVC6
                    // find out if this is a source system or bin system (determined by location of YetaWF.Core.dll)
                    if (File.Exists(Path.Combine(YetaWFManager.RootFolderWebProject, AreaRegistration.CurrentPackage.PackageAssembly.GetName().Name + ".dll"))) {
                        // Published (w/o source by definition)
                        string sourceBin = Path.Combine(tempBin, "bin", "Release", "net46");
                        CopyVersionedFiles(sourceBin, Path.Combine(YetaWFManager.RootFolderWebProject));
                        CopyVersionedFiles(sourceBin, Path.Combine(YetaWFManager.RootFolderWebProject, "refs"));
                    } else {
                        // Dev (with or without source code)
                        bool copied = false;
                        string binPath;
                        binPath = Path.Combine(YetaWFManager.RootFolderWebProject, "bin", "Debug", "net46", "win7-x64");
                        string sourceBin = Path.Combine(tempBin, "bin", "Release", "net46");
                        if (Directory.Exists(binPath)) {
                            CopyVersionedFiles(sourceBin, binPath);
                            copied = true;
                        }
                        binPath = Path.Combine(YetaWFManager.RootFolderWebProject, "bin", "Release", "net46", "win7-x64");
                        if (Directory.Exists(binPath)) {
                            CopyVersionedFiles(sourceBin, binPath);
                            copied = true;
                        }
                        if (!copied) {
                            if (!hasSource)
                                throw new Error("Package import ({0}) failed because the target location {1} doesn't exist. Packages", serPackage.PackageName, binPath);
                        }
                    }
#else
                    string sourceBin = Path.Combine(tempBin, "bin");
                    CopyVersionedFiles(sourceBin, Path.Combine(YetaWFManager.RootFolder, "Bin"));
#endif
                    try {// try to delete all dirs up to and including tempbin if empty (ignore any errors)
                        Directory.Delete(Path.Combine(YetaWFManager.RootFolderWebProject, "tempbin", serPackage.PackageDomain, serPackage.PackageProduct), true);
                        Directory.Delete(Path.Combine(YetaWFManager.RootFolderWebProject, "tempbin", serPackage.PackageDomain));
                        Directory.Delete(Path.Combine(YetaWFManager.RootFolderWebProject, "tempbin"));
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
                        string rootFolder;
#if MVC6
                        rootFolder = YetaWFManager.RootFolderWebProject;
#else
                        rootFolder = YetaWFManager.RootFolder;
#endif
                        e.Extract(rootFolder, ExtractExistingFileAction.OverwriteSilently);
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

#if NOTNEEDED // this is automatically created when the site restarts
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
#endif
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
                        return; // no need to copy, target version >= source version
                }
            }
            DateTime modTarget = File.GetLastWriteTimeUtc(targetFile);
            DateTime modSource = File.GetLastWriteTimeUtc(sourceFile);
            if (modTarget >= modSource)
                return; // no need to copy, target modified date/time >= source modified
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
        public static bool IsPackageSymLink(string folder) {
            return Junction.Exists(folder);
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
