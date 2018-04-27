/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Modules;
using YetaWF.Core.Pages;
using YetaWF.Core.Serializers;
using YetaWF.Core.Support;
using YetaWF.Core.Support.Zip;
using YetaWF.PackageAttributes;

namespace YetaWF.Core.Packages {

    public class SerializablePackage {
        public string PackageName { get; set; }
        public string PackageVersion { get; set; }
        public PackageTypeEnum PackageType { get; set; }
        public string CoreVersion { get; set; }
        public YetaWFManager.AspNetMvcVersion AspNetMvcVersion { get; set; }

        public SerializableList<SerializableFile> BinFiles { get; set; }
        public SerializableList<SerializableFile> AddOns { get; set; }
        public SerializableList<SerializableFile> Views { get; set; }
        public SerializableList<SerializableFile> SourceFiles { get; set; }
        public string PackageDomain { get; set; }
        public string PackageProduct { get; set; }

        public SerializablePackage() {
            BinFiles = new SerializableList<SerializableFile>();
            AddOns = new SerializableList<SerializableFile>();
            Views = new SerializableList<SerializableFile>();
            SourceFiles = new SerializableList<SerializableFile>();
        }
    }

    public class SerializablePage {
        public string PageUrl { get; set; }
        public string CoreVersion { get; set; }

        public Guid PageGuid { get; set; }
        public PageDefinition PageDef { get; set; }
        public SerializableList<string> ModuleZips { get; set; }
        public SerializableList<SerializableFile> Files { get; set; }

        public SerializablePage() {
            ModuleZips = new SerializableList<string>();
            Files = new SerializableList<SerializableFile>();
        }
    }

    public class SerializableModule {
        public string ModuleName { get; set; }
        public string ModuleVersion { get; set; }
        public string CoreVersion { get; set; }

        public Guid ModuleGuid { get; set; }
        public ModuleDefinition ModDef { get; set; }
        public SerializableList<SerializableFile> Files { get; set; }

        public SerializableModule() { Files = new SerializableList<SerializableFile>(); }
    }

    public class SerializableData {
        public string PackageName { get; set; }
        public string PackageVersion { get; set; }

        public SerializableList<SerializableModelData> Data { get; set; }
    }

    public class SerializableModelData {
        public string Class { get; set; }
        public int Chunks { get; set; }
        public SerializableList<SerializableFile> Files { get; set; }

        public SerializableModelData() { Files = new SerializableList<SerializableFile>(); }
    }

    public class SerializableFile {

        public SerializableFile() { }
        public SerializableFile(string fileName, string ExternalRoot = null) {
            Uri file = new Uri(fileName);
            if (!file.IsFile)
                throw new InternalError("{0} is not a valid filename and cannot be exported", fileName);
            fileName = file.LocalPath;
            string relFileName = fileName;
            if (string.IsNullOrWhiteSpace(ExternalRoot)) {
                string rootFolder;
#if MVC6
                rootFolder = YetaWFManager.RootFolderWebProject;
#else
                rootFolder = YetaWFManager.RootFolder;
#endif
                ExternalRoot = rootFolder;
            }
            if (!fileName.StartsWith(ExternalRoot, StringComparison.OrdinalIgnoreCase))
                throw new InternalError("'{0}' is not within the folder '{1}' and cannot be exported.", fileName, ExternalRoot);

            if (YetaWFManager.HaveManager && fileName.StartsWith(YetaWFManager.Manager.SiteFolder, StringComparison.OrdinalIgnoreCase)) {
                SiteSpecific = true;
                relFileName = fileName.Substring(YetaWFManager.Manager.SiteFolder.Length);
            } else {
                relFileName = fileName.Substring(ExternalRoot.Length);
            }
            AbsFileName = fileName;
            FileName = relFileName;
        }

        public string FileName { get; private set; }
        public DateTime FileDate { get; set; }
        public bool SiteSpecific { get; private set; }
        [DontSave]
        public string AbsFileName { get; private set; }

        public void ReplaceAbsFileName(string fileName) {
            AbsFileName = fileName;
        }
    }
}
