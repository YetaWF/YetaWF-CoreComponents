/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;
using YetaWF.Core.Models;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Skins {

    public class PageSkinList : List<PageSkinEntry> { }
    public class PageSkinEntry {
        public string Name { get; set; }
        public string FileName { get; set; }
        public string Description { get; set; }
        public string Css { get; set; }
        public int Width { get; set; } // popup width
        public int Height { get; set; } // popup width
        public bool MaximizeButton { get; set; } // popup has maximize button
        public int CharWidthAvg { get; set; }
        public int CharHeight { get; set; }
    }

    public class ModuleSkinList : List<ModuleSkinEntry> { }
    public class ModuleSkinEntry {
        public string Name { get; set; }
        public string FileName { get; set; }
        public string CssClass { get; set; }
        public string Description { get; set; }
        public int CharWidthAvg { get; set; }
        public int CharHeight { get; set; }
    }

    public class SkinCollectionInfoList : List<SkinCollectionInfo> { }
    public class SkinCollectionInfo {
        public string CollectionName { get; set; }
        public string CollectionDescription { get; set; }
        public string JQuerySkin { get; set; }
        public string KendoSkin { get; set; }
        public bool UsingBootstrap { get; set; }
        public bool UseDefaultBootstrap { get; set; }
        public bool UsingBootstrapButtons { get; set; }
        public string PartialFormCss { get; set; }
        public int MinWidthForPopups { get; set; }
        public PageSkinList PageSkins { get; set; }
        public PageSkinList PopupSkins { get; set; }
        public ModuleSkinList ModuleSkins { get; set; }

        public string Folder { get; set; }
        public SkinCollectionInfo() {
            PageSkins = new PageSkinList();
            PopupSkins = new PageSkinList();
            ModuleSkins = new ModuleSkinList();
        }
    }

    // Skin definition for a page, popup or module
    public class SkinDefinition {

        protected static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        public const int MaxCollection = 100;
        public const int MaxName = 100;
        public const int MaxSkinFile = 100;

        public SkinDefinition() { }

        [StringLength(MaxCollection)]
        public string Collection { get; set; } // may be null for site default

        [StringLength(MaxSkinFile)]
        public string FileName { get; set; } // may be null for site default

        public static SkinDefinition EvaluatedSkin(PageDefinition page, bool popup) {
            SkinDefinition skin = (Manager.IsInPopup) ? page.SelectedPopupSkin : page.SelectedSkin;
            return EvaluatedSkin(skin, popup);
        }
        public static SkinDefinition EvaluatedSkin(SkinDefinition skin, bool popup) {
            string fileName = skin.FileName;
            string collection = skin.Collection;
            if (string.IsNullOrWhiteSpace(collection)) {
                if (popup) {
                    collection = Manager.CurrentSite.SelectedPopupSkin.Collection;
                } else {
                    collection = Manager.CurrentSite.SelectedSkin.Collection;
                }
            }
            if (string.IsNullOrWhiteSpace(collection)) {
                if (popup) {
                    collection = SkinAccess.FallbackPopupSkinCollectionName;
                } else {
                    collection = SkinAccess.FallbackSkinCollectionName;
                }
            }
            if (string.IsNullOrWhiteSpace(fileName)) {
                if (popup) {
                    collection = Manager.CurrentSite.SelectedPopupSkin.Collection;
                    fileName = Manager.CurrentSite.SelectedPopupSkin.FileName;
                } else {
                    collection = Manager.CurrentSite.SelectedSkin.Collection;
                    fileName = Manager.CurrentSite.SelectedSkin.FileName;
                }
                if (string.IsNullOrWhiteSpace(collection)) {
                    if (popup) {
                        collection = SkinAccess.FallbackPopupSkinCollectionName;
                        fileName = SkinAccess.FallbackPopupFileName;
                    } else {
                        collection = SkinAccess.FallbackSkinCollectionName;
                        fileName = SkinAccess.FallbackPageFileName;
                    }
                }
            }
            return new SkinDefinition {
                Collection = collection,
                FileName = fileName,
            };
        }

        public SkinDefinition(SkinDefinition copy) {
            ObjectSupport.CopyData(copy, this);
        }

        public static SkinDefinition FallbackSkin {
            get {
                if (_fallbackSkin == null)
                    _fallbackSkin = new SkinDefinition {
                        Collection = SkinAccess.FallbackSkinCollectionName,
                        FileName = SkinAccess.FallbackPageFileName,
                    };
                return _fallbackSkin;
            }
        }
        static SkinDefinition _fallbackSkin;

        public static SkinDefinition FallbackPopupSkin {
            get {
                if (_fallbackPopupSkin == null)
                    _fallbackPopupSkin = new SkinDefinition {
                        Collection = SkinAccess.FallbackPopupSkinCollectionName,
                        FileName = SkinAccess.FallbackPopupFileName,
                    };
                return _fallbackPopupSkin;
            }
        }
        static SkinDefinition _fallbackPopupSkin;
    }
}
