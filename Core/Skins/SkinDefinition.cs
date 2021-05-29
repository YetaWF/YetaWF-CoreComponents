/* Copyright © 2021 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;
using YetaWF.Core.DataProvider.Attributes;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Packages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Skins {

    public class PageSkinList : List<PageSkinEntry> { }
    public class PageSkinEntry {
        [System.ComponentModel.DataAnnotations.Required]
        public string Name { get; set; } = null!;
        [System.ComponentModel.DataAnnotations.Required]
        public string ViewName { get; set; } = null!;
        [System.ComponentModel.DataAnnotations.Required]
        public string Description { get; set; } = null!;
        [System.ComponentModel.DataAnnotations.Required]
        public string CSS { get; set; } = null!;
        [System.ComponentModel.DataAnnotations.Required]
        public int Width { get; set; } // popup width
        [System.ComponentModel.DataAnnotations.Required]
        public int Height { get; set; } // popup width
        public bool MaximizeButton { get; set; } // popup has maximize button RFFU
    }

    public class ModuleSkinList : List<ModuleSkinEntry> { }
    public class ModuleSkinEntry {
        [System.ComponentModel.DataAnnotations.Required]
        public string Name { get; set; } = null!;
        [System.ComponentModel.DataAnnotations.Required]
        public string CSS { get; set; } = null!;
        [System.ComponentModel.DataAnnotations.Required]
        public string Description { get; set; } = null!;
    }

    public class SkinCollectionInfoList : List<SkinCollectionInfo> { }
    public class SkinCollectionInfo {
        public Package Package { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string AreaName { get; set; } = null!;
        public string Folder { get; set; } = null!;

        public string Url { 
            get {
                if (_url == null)
                    _url = Utility.PhysicalToUrl(Folder) + "/";
                return _url;
            } 
        }
        private string? _url = null;

        [System.ComponentModel.DataAnnotations.Required]
        public string Description { get; set; } = null!;
        public string? PartialFormCss { get; set; }
        public int MinWidthForPopups { get; set; }
        public int MinWidthForCondense { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public PageSkinList PageSkins { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public PageSkinList PopupSkins { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public ModuleSkinList ModuleSkins { get; set; }

        public SkinCollectionInfo() {
            PageSkins = new PageSkinList();
            PopupSkins = new PageSkinList();
            ModuleSkins = new ModuleSkinList();
        }
    }

    public class SkinDefinition {

        protected static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        public const int MaxCollection = 100;
        public const int MaxPageFile = 100;
        public const int MaxPopupFile = 100;

        public SkinDefinition() { }

        [StringLength(MaxCollection)]
        [Data_NewValue]
        public string Collection { get; set; } = null!;

        [StringLength(MaxPageFile)]
        [Data_NewValue]
        public string PageFileName { get; set; } = null!;
        [StringLength(MaxPopupFile)]
        [Data_NewValue]
        public string PopupFileName { get; set; } = null!;

        public static SkinDefinition EvaluatedSkin() {
            return Manager.CurrentSite.Skin;
        }

        public static SkinDefinition FallbackSkin {
            get {
                if (_fallbackSkin == null)
                    _fallbackSkin = new SkinDefinition {
                        Collection = SkinAccess.FallbackSkinCollectionName,
                        PageFileName = SkinAccess.FallbackPageFileName,
                        PopupFileName = SkinAccess.FallbackPopupFileName,
                    };
                return _fallbackSkin;
            }
        }
        static SkinDefinition? _fallbackSkin;
    }
}
