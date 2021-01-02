/* Copyright © 2021 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;
using YetaWF.Core.DataProvider.Attributes;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Support;

namespace YetaWF.Core.Skins {

    public class PageSkinList : List<PageSkinEntry> { }
    public class PageSkinEntry {
        public string Name { get; set; } = null!;
        public string PageViewName { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Css { get; set; } = null!;
        public int Width { get; set; } // popup width
        public int Height { get; set; } // popup width
        public bool MaximizeButton { get; set; } // popup has maximize button
    }

    public class ModuleSkinList : List<ModuleSkinEntry> { }
    public class ModuleSkinEntry {
        public string Name { get; set; } = null!;
        public string CssClass { get; set; } = null!;
        public string Description { get; set; } = null!;
        public int CharWidthAvg { get; set; }
        public int CharHeight { get; set; }
    }

    public class SkinCollectionInfoList : List<SkinCollectionInfo> { }
    public class SkinCollectionInfo {
        public string CollectionName { get; set; } = null!;
        public string CollectionDescription { get; set; } = null!;
        public string? JQuerySkin { get; set; }
        public string? KendoSkin { get; set; }
        public bool UsingBootstrap { get; set; }
        public bool UseDefaultBootstrap { get; set; }
        public bool UsingBootstrapButtons { get; set; }
        public string? PartialFormCss { get; set; }
        public int MinWidthForPopups { get; set; }
        public int MinWidthForCondense { get; set; }
        public PageSkinList PageSkins { get; set; }
        public PageSkinList PopupSkins { get; set; }
        public ModuleSkinList ModuleSkins { get; set; }
        public string AreaName { get; set; } = null!;
        public string Folder { get; set; } = null!;

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
