/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Modules;
using YetaWF.Core.Pages;
using YetaWF.Core.Serializers;
using YetaWF.Core.Support;
using YetaWF.PackageAttributes;

namespace YetaWF.Core.Packages {

    public class SerializablePackage {
        public string PackageName { get; set; } = null!;
        public string PackageVersion { get; set; } = null!;
        public PackageTypeEnum PackageType { get; set; }
        public string CoreVersion { get; set; } = null!;
        public Utility.AspNetMvcVersion AspNetMvcVersion { get; set; }

        public SerializableList<SerializableFile> BinFiles { get; set; }
        public SerializableList<SerializableFile> AddOns { get; set; }
        public SerializableList<SerializableFile> LocalizationFiles { get; set; }
        public SerializableList<SerializableFile> SourceFiles { get; set; }
        public string PackageDomain { get; set; } = null!;
        public string PackageProduct { get; set; } = null!;

        public SerializablePackage() {
            BinFiles = new SerializableList<SerializableFile>();
            AddOns = new SerializableList<SerializableFile>();
            LocalizationFiles = new SerializableList<SerializableFile>();
            SourceFiles = new SerializableList<SerializableFile>();
        }
    }

    public class SerializablePage {
        public string PageUrl { get; set; } = null!;
        public string CoreVersion { get; set; } = null!;

        public Guid PageGuid { get; set; }
        public PageDefinition PageDef { get; set; } = null!;
        public SerializableList<string> ModuleZips { get; set; }
        public SerializableList<SerializableFile> Files { get; set; }
        public SerializableList<RoleLookupEntry> Roles { get; set; }
        public SerializableList<UserLookupEntry> Users { get; set; }

        public SerializablePage() {
            ModuleZips = new SerializableList<string>();
            Files = new SerializableList<SerializableFile>();
            Roles = new SerializableList<RoleLookupEntry>();
            Users = new SerializableList<UserLookupEntry>();
        }
    }

    public class SerializableModule {
        public string ModuleName { get; set; } = null!;
        public string ModuleVersion { get; set; } = null!;
        public string CoreVersion { get; set; } = null!;

        public Guid ModuleGuid { get; set; }
        public ModuleDefinition ModDef { get; set; } = null!;
        public SerializableList<SerializableFile> Files { get; set; }
        public SerializableList<RoleLookupEntry> Roles { get; set; }
        public SerializableList<UserLookupEntry> Users { get; set; }

        public SerializableModule() {
            Files = new SerializableList<SerializableFile>();
            Roles = new SerializableList<RoleLookupEntry>();
            Users = new SerializableList<UserLookupEntry>();
        }
    }

    public class RoleLookupEntry {
        public string RoleName { get; set; } = null!; // the role name on the originating system
        public int RoleId { get; set; } // the role id on the originating system
    }
    public class UserLookupEntry {
        public string UserName { get; set; } = null!; // the user name on the originating system
        public int UserId { get; set; } // the user id on the originating system
    }

    public class SerializableData {
        public string PackageName { get; set; } = null!;
        public string PackageVersion { get; set; } = null!;
        public SerializableList<RoleLookupEntry> Roles { get; set; }
        public SerializableList<UserLookupEntry> Users { get; set; }

        public SerializableList<SerializableModelData> Data { get; set; } = null!;

        public SerializableData() {
            Roles = new SerializableList<RoleLookupEntry>();
            Users = new SerializableList<UserLookupEntry>();
        }
    }

    public class SerializableModelData {
        public string Class { get; set; } = null!;
        public int Chunks { get; set; }
        public SerializableList<SerializableFile> Files { get; set; }

        public SerializableModelData() { Files = new SerializableList<SerializableFile>(); }
    }

    public class SerializableFile {

        public SerializableFile() { }
        public SerializableFile(string fileName, string? ExternalRoot = null) {
            Uri file = new Uri(fileName);
            if (!file.IsFile)
                throw new InternalError("{0} is not a valid filename and cannot be exported", fileName);
            fileName = file.LocalPath;
            if (string.IsNullOrWhiteSpace(ExternalRoot)) {
                string rootFolder = YetaWFManager.RootFolderWebProject;
                ExternalRoot = rootFolder;
            }
            if (!fileName.StartsWith(ExternalRoot, StringComparison.OrdinalIgnoreCase))
                throw new InternalError("'{0}' is not within the folder '{1}' and cannot be exported.", fileName, ExternalRoot);

            string relFileName;
            if (YetaWFManager.HaveManager && fileName.StartsWith(YetaWFManager.Manager.SiteFolder, StringComparison.OrdinalIgnoreCase)) {
                SiteSpecific = true;
                relFileName = fileName.Substring(YetaWFManager.Manager.SiteFolder.Length);
            } else {
                relFileName = fileName.Substring(ExternalRoot.Length);
            }
            AbsFileName = fileName;
            FileName = relFileName;
        }

        public string FileName { get; set; } = null!;
        public DateTime FileDate { get; set; }
        public bool SiteSpecific { get; set; }
        [DontSave]
        public string AbsFileName { get; private set; } = null!;

        public void ReplaceAbsFileName(string fileName) {
            AbsFileName = fileName;
        }
    }
}
