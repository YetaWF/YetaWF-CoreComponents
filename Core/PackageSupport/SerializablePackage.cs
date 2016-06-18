/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.IO;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Modules;
using YetaWF.Core.Serializers;
using YetaWF.Core.Support;
using YetaWF.PackageAttributes;

namespace YetaWF.Core.Packages {

    public class SerializablePackage {
        public string PackageName { get; set; }
        public string PackageVersion { get; set; }
        public PackageTypeEnum PackageType { get; set; }
        public string CoreVersion { get; set; }

        public SerializableList<SerializableFile> BinFiles { get; set; }
        public SerializableList<SerializableFile> AddOns { get; set; }
        public SerializableList<SerializableFile> SourceFiles { get; set; }
        public string PackageDomain { get; set; }
        public string PackageProduct { get; set; }

        public SerializablePackage() {
            BinFiles = new SerializableList<SerializableFile>();
            AddOns = new SerializableList<SerializableFile>();
            SourceFiles = new SerializableList<SerializableFile>();
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
            if (string.IsNullOrWhiteSpace(ExternalRoot))
                ExternalRoot = YetaWFManager.RootFolder;
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
            FileDate = File.GetCreationTimeUtc(AbsFileName);
        }

        public string FileName { get; private set; }
        public DateTime FileDate { get; private set; }
        public bool SiteSpecific { get; private set; }
        [DontSave]
        public string AbsFileName { get; private set; }
    }
}
