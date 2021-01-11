/* Copyright © 2021 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YetaWF.Core.Addons;
using YetaWF.Core.Components;
using YetaWF.Core.Identity;
using YetaWF.Core.IO;
using YetaWF.Core.Localize;
using YetaWF.Core.Modules;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Skins {

    public partial class SkinAccess {

        public const string FallbackSkinCollectionName = "YetaWF/Core/Standard";
        public const string FallbackPageFileName = "Default";
        public const string FallbackPagePlainFileName = "Plain";
        public const string FallbackPopupFileName = "Popup";
        public const string FallbackPopupMediumFileName = "PopupMedium";
        public const string FallbackPopupSmallFileName = "PopupSmall";

        public SkinAccess() {  }

        protected YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        public string GetViewName() {
            SkinDefinition skin = Manager.CurrentSite.Skin;
            SkinCollectionInfo? info = TryFindSkinCollection(skin.Collection);
            if (info == null)
                info = FindSkinCollection(SkinAccess.FallbackSkinCollectionName);
            return $"{info.AreaName}_{(Manager.IsInPopup ? skin.PopupFileName : skin.PageFileName)}";
        }

        // Returns the panes defined by the page skin
        public List<string> GetPanes() {
            return YetaWFPageExtender.GetPanes(GetViewName());
        }

        public SkinCollectionInfo GetSkinCollectionInfo() {
            SkinDefinition skin = Manager.CurrentSite.Skin;
            SkinCollectionInfo? info = TryFindSkinCollection(skin.Collection);
            if (info == null)
                info = FindSkinCollection(SkinAccess.FallbackSkinCollectionName);
            return info;
        }

        public PageSkinEntry GetPageSkinEntry() {
            SkinDefinition skin = Manager.CurrentSite.Skin;
            SkinCollectionInfo? info = TryFindSkinCollection(skin.Collection);
            PageSkinEntry? pageSkinEntry;
            if (info == null) {
                info = FindSkinCollection(SkinAccess.FallbackSkinCollectionName);
                pageSkinEntry = Manager.IsInPopup ? info.PopupSkins.First() : info.PageSkins.First();
            } else {
                string fileName;
                if (Manager.IsInPopup) {
                    fileName = skin.PopupFileName;
                    if (string.IsNullOrWhiteSpace(fileName))
                        fileName = FallbackPopupFileName;
                    pageSkinEntry = (from s in info.PopupSkins where s.ViewName == fileName select s).FirstOrDefault();
                } else {
                    fileName = skin.PageFileName;
                    if (string.IsNullOrWhiteSpace(fileName))
                        fileName = FallbackPageFileName;
                    pageSkinEntry = (from s in info.PageSkins where s.ViewName == fileName select s).FirstOrDefault();
                }
            }
            if (pageSkinEntry == null)
                throw new InternalError("No page skin found");
            return pageSkinEntry;
        }
        private ModuleSkinEntry GetModuleSkinEntry(ModuleDefinition mod) {
            // Get the skin info for the page's skin collection and get the default module skin
            SkinDefinition skin = Manager.CurrentSite.Skin;
            SkinCollectionInfo? info = TryFindSkinCollection(skin.Collection);
            ModuleSkinEntry? modSkinEntry;
            if (info == null) {
                info = FindSkinCollection(SkinAccess.FallbackSkinCollectionName);
                modSkinEntry = info.ModuleSkins.First();
            } else {
                // Find the module skin name to use for the page's skin collection
                modSkinEntry = info.ModuleSkins.First();
                if (modSkinEntry == null) throw new InternalError("No module skin found");
            }
            return modSkinEntry;
        }

        internal async Task<string> MakeModuleContainerAsync(ModuleDefinition mod, string htmlContents, bool ShowMenu = true, bool ShowTitle = true, bool ShowAction = true) {
            ModuleSkinEntry modSkinEntry = GetModuleSkinEntry(mod);
            string modSkinCss = modSkinEntry.CSS;

            HtmlBuilder hb = new HtmlBuilder();

            if (!mod.IsModuleUnique && Manager.CurrentPage != null && !string.IsNullOrWhiteSpace(mod.AnchorId)) { 
                // add an anchor
                hb.Append($@"<div class='yAnchor' id='{mod.AnchorId}'></div>");
            }

            string css = string.Empty;
            if (!string.IsNullOrWhiteSpace(mod.CssClass) && !Manager.EditMode)
                css = CssManager.CombineCss(css, Manager.AddOnManager.CheckInvokedCssModule(mod.CssClass));
            if (!mod.Print)
                css = CssManager.CombineCss(css, Manager.AddOnManager.CheckInvokedCssModule(Globals.CssModuleNoPrint));

            // add css classes to modules that can't be seen by anonymous users and users
            bool showOwnership = UserSettings.GetProperty<bool>("ShowModuleOwnership") && await Resource.ResourceAccess.IsResourceAuthorizedAsync(CoreInfo.Resource_ViewOwnership);
            if (showOwnership) {
                bool anon = mod.IsAuthorized_View_Anonymous();
                bool user = mod.IsAuthorized_View_AnyUser();
                if (!anon && !user) {
                    css = CssManager.CombineCss(css, "ymodrole_noUserAnon");
                } else if (!anon) {
                    css = CssManager.CombineCss(css, "ymodrole_noAnon");
                } else if (!user) {
                    css = CssManager.CombineCss(css, "ymodrole_noUser");
                }
            }

            hb.Append($@"
<div id='{mod.ModuleHtmlId}' data-moduleguid='{mod.ModuleGuid.ToString()}' class='{Manager.AddOnManager.CheckInvokedCssModule(Globals.CssModule)} {Manager.AddOnManager.CheckInvokedCssModule(mod.AreaName)} {Manager.AddOnManager.CheckInvokedCssModule(mod.AreaName + "_" + mod.ModuleName)} {Manager.AddOnManager.CheckInvokedCssModule(modSkinCss)} {css}'>");

            if (ShowMenu && mod.ShowModuleMenu) {
                hb.Append(await YetaWFCoreRendering.Render.RenderModuleMenuAsync(mod));
            }
            if (ShowTitle) {
                if (mod.ShowTitleActions) {
                    string? actions = null;
                    if (ShowTitle && mod.ShowTitleActions)
                        actions = await YetaWFCoreRendering.Render.RenderModuleLinksAsync(mod, ModuleAction.RenderModeEnum.IconsOnly, Globals.CssModuleLinksContainer);
                    if (!string.IsNullOrWhiteSpace(actions)) {
                        hb.Append($@"
    <div class='yModuleTitle'>
         <h1>{Utility.HE(mod.Title)}</h1>
        {actions}
    </div>
    <div class='y_cleardiv'></div>");
                    } else {
                        hb.Append($@"<h1>{Utility.HE(mod.Title)}</h1>");
                    }
                } else {
                    hb.Append($@"<h1>{Utility.HE(mod.Title)}</h1>");
                }
            }

            hb.Append($@"
{htmlContents}");

            if (ShowAction && !string.IsNullOrWhiteSpace(Manager.PaneRendered)) { // only show action menus in a pane
                if (mod.ShowActionMenu)
                    hb.Append($@"
{await YetaWFCoreRendering.Render.RenderModuleLinksAsync(mod, ModuleAction.RenderModeEnum.NormalLinks, Globals.CssModuleLinksContainer)}");
            }

            hb.Append($@"
</div>");
                return hb.ToString();
        }

        /// <summary>
        /// Get all skin collections
        /// </summary>
        /// <returns></returns>
        public SkinCollectionInfoList GetAllSkinCollections() {
            if (_skinCollections == null) {
                List<VersionManager.AddOnProduct> addonSkinColls = VersionManager.GetAvailableSkinCollections();
                SkinCollectionInfoList newList = new SkinCollectionInfoList();
                foreach (VersionManager.AddOnProduct addon in addonSkinColls) {
                    newList.Add(addon.SkinInfo);
                }
                _skinCollections = newList;
            }
            return _skinCollections;
        }
        private static SkinCollectionInfoList? _skinCollections;

        /// <summary>
        /// Find a skin collection.
        /// </summary>
        /// <returns></returns>
        protected SkinCollectionInfo? TryFindSkinCollection(string collection) {
            return (from c in VersionManager.GetAvailableSkinCollections() where c.SkinInfo.Name == collection select c.SkinInfo).FirstOrDefault();
        }
        /// <summary>
        /// Find a skin collection.
        /// </summary>
        /// <returns></returns>
        protected SkinCollectionInfo FindSkinCollection(string collection) {
            SkinCollectionInfo? info = TryFindSkinCollection(collection);
            if (info == null)
                throw new InternalError("Skin collection {0} not found", collection);
            return info;
        }

        /// <summary>
        /// Get all page skins for a collection
        /// </summary>
        /// <param name="skinCollection"></param>
        /// <returns></returns>
        public PageSkinList GetAllPageSkins(string skinCollection) {
            if (string.IsNullOrWhiteSpace(skinCollection)) {
                PageSkinList skinList = new PageSkinList {
                    new PageSkinEntry {
                        Name = this.__ResStr("page", "Default Page"),
                        Description = this.__ResStr("pageTT", "Default Page"),
                        ViewName = FallbackPageFileName,
                    },
                    new PageSkinEntry {
                        Name = this.__ResStr("pagePlain", "Plain Page"),
                        Description = this.__ResStr("pagePlainTT", "Plain Page"),
                        ViewName = FallbackPagePlainFileName,
                    },
                };
                return skinList;
            } else
                return GetAllSkins(skinCollection);
        }

        /// <summary>
        /// Get all popup skins for a collection
        /// </summary>
        /// <param name="skinCollection"></param>
        /// <returns></returns>
        public PageSkinList GetAllPopupSkins(string skinCollection) {
            if (string.IsNullOrWhiteSpace(skinCollection)) {
                PageSkinList skinList = new PageSkinList {
                    new PageSkinEntry {
                        Name = this.__ResStr("pop", "Popup (Default)"),
                        Description = this.__ResStr("popTT", "Large popup"),
                        ViewName = FallbackPopupFileName,
                    },
                    new PageSkinEntry {
                        Name = this.__ResStr("popMed", "Medium Popup"),
                        Description = this.__ResStr("popMedTT", "Medium popup"),
                        ViewName = FallbackPopupMediumFileName,
                    },
                    new PageSkinEntry {
                        Name = this.__ResStr("popSmall", "Small Popup"),
                        Description = this.__ResStr("popSmallTT", "Small popup"),
                        ViewName = FallbackPopupSmallFileName,
                    }
                };
                return skinList;
            } else
                return GetAllSkins(skinCollection, Popup: true);
        }

        private PageSkinList GetAllSkins(string skinCollection, bool Popup = false) {
            VersionManager.AddOnProduct addon = VersionManager.FindSkinVersion(skinCollection);
            return Popup ? addon.SkinInfo.PopupSkins : addon.SkinInfo.PageSkins;
        }

        /// <summary>
        /// Get all module skins for a collection
        /// </summary>
        /// <param name="skinCollection"></param>
        /// <returns></returns>
        public ModuleSkinList GetAllModuleSkins(string skinCollection) {
            VersionManager.AddOnProduct addon = VersionManager.FindSkinVersion(skinCollection);
            return addon.SkinInfo.ModuleSkins;
        }

        public async Task<List<string>> GetThemesAsync() {
            List<string> list = new List<string>();
            SkinCollectionInfo skinInfo = FindSkinCollection(Manager.CurrentSite.Skin.Collection);
            foreach (string themePath in await FileSystem.FileSystemProvider.GetFilesAsync(Path.Combine(skinInfo.Folder, "Themes"), "*.css")) {
                list.Add(Path.GetFileNameWithoutExtension(themePath));
            }
            return list;
        }
    }
}
