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
using YetaWF.Core.Packages;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Skins {

    public partial class SkinAccess {

        public const string FallbackSkinCollectionName = "YetaWF/Core/Standard";
        public const string FallbackPageFileName = SkinAccess.PAGE_VIEW_DEFAULT;
        public const string FallbackPagePlainFileName = SkinAccess.PAGE_VIEW_PLAIN;
        public const string FallbackPopupFileName = SkinAccess.POPUP_VIEW_DEFAULT;
        public const string FallbackPopupMediumFileName = SkinAccess.POPUP_VIEW_MEDIUM;
        public const string FallbackPopupSmallFileName = SkinAccess.POPUP_VIEW_SMALL;

        protected YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        public SkinAccess() {  }

        public static SkinCollectionInfo FallbackSkinCollectionInfo {
            get {
                if (_fallbackSkinCollectionInfo == null) {
                    SkinAccess skinAccess = new SkinAccess();
                    _fallbackSkinCollectionInfo = skinAccess.FindSkinCollection(SkinAccess.FallbackSkinCollectionName);
                }
                return _fallbackSkinCollectionInfo;
            }
        }
        private static SkinCollectionInfo? _fallbackSkinCollectionInfo = null;

        public string GetViewName(string? popupPage) {
            SkinDefinition skin = Manager.CurrentSite.Skin;
            SkinCollectionInfo info = TryFindSkinCollection(skin.Collection) ?? FallbackSkinCollectionInfo;
            return $"{info.AreaName}_{(Manager.IsInPopup ? popupPage ?? skin.PopupFileName : skin.PageFileName)}";
        }

        // Returns the panes defined by the page skin
        public List<string> GetPanes(string? popupPage) {
            return YetaWFPageExtender.GetPanes(GetViewName(popupPage));
        }

        public SkinCollectionInfo GetSkinCollectionInfo() {
            SkinDefinition skin = Manager.CurrentSite.Skin;
            SkinCollectionInfo info = TryFindSkinCollection(skin.Collection) ?? FallbackSkinCollectionInfo;
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
                string? fileName;
                if (Manager.IsInPopup) {
                    fileName = Manager.CurrentPage.PopupPage;
                    if (string.IsNullOrWhiteSpace(fileName))
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
            if (info == null)
                info = FindSkinCollection(SkinAccess.FallbackSkinCollectionName);

            // Find the module skin name to use for the page's skin collection
            string find = mod.ModuleSkin ?? SkinAccess.MODULE_SKIN_DEFAULT;
            ModuleSkinEntry? modSkinEntry = (from s in info.ModuleSkins where s.CSS == find select s).FirstOrDefault();
            if (modSkinEntry == null) throw new InternalError("No module skin found");

            return modSkinEntry;
        }

        internal async Task<string> MakeModuleContainerAsync(ModuleDefinition mod, string htmlContents, bool ShowTitle = true) {

            HtmlBuilder hb = new HtmlBuilder();

            if (!mod.IsModuleUnique && Manager.CurrentPage != null && !string.IsNullOrWhiteSpace(mod.AnchorId)) { 
                // add an anchor
                hb.Append($@"<div class='yAnchor' id='{mod.AnchorId}'></div>");
            }

            ModuleSkinEntry modSkinEntry = GetModuleSkinEntry(mod);

            string name = modSkinEntry.CSS;
            if (Manager.IsInPopup) // force standard for popups
                name = SkinAccess.MODULE_SKIN_DEFAULT;

            string css = name;
            css = CssManager.CombineCss(css, Globals.CssModule);
            css = CssManager.CombineCss(css, mod.AreaName + "_" + mod.ModuleName);
            css = CssManager.CombineCss(css, mod.AreaName);
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

            switch (name) {
                default:
                case SkinAccess.MODULE_SKIN_DEFAULT:
                    await RenderStandardModuleAsync(mod, hb, htmlContents, css, ShowTitle);
                    break;
                case SkinAccess.MODULE_SKIN_PANEL:
                    await RenderPanelModuleAsync(mod, hb, htmlContents, css, ShowTitle);
                    break;
            }
            return hb.ToString();
        }

        private async Task RenderStandardModuleAsync(ModuleDefinition mod, HtmlBuilder hb, string htmlContents, string css, bool showTitle = true) {

            hb.Append($@"
<div id='{mod.ModuleHtmlId}' data-moduleguid='{mod.ModuleGuid.ToString()}' class='{css}'>");

            if (!Manager.IsInPopup && mod.ShowModuleMenu) {
                hb.Append(await YetaWFCoreRendering.Render.RenderModuleMenuAsync(mod));
            }
            if (showTitle) {

                hb.Append($@"
    <div class='yModuleTitle'>
         <h1>{Utility.HE(mod.Title)}</h1>");

                if (mod.ShowTitleActions) {
                    string? actions = null;
                    if (showTitle && mod.ShowTitleActions)
                        actions = await YetaWFCoreRendering.Render.RenderModuleLinksAsync(mod, ModuleAction.RenderModeEnum.IconsOnly, Globals.CssModuleLinksContainer);
                    if (!string.IsNullOrWhiteSpace(actions)) {
                        hb.Append($@"
        {actions}");
                    }
                }

                hb.Append($@"
    </div>
    <div class='y_cleardiv'></div>");
            }

            hb.Append($@"
    <div class='yModuleContents'>
{htmlContents}
    </div>");

            if (!string.IsNullOrWhiteSpace(Manager.PaneRendered)) { // only show action menus in a pane
                if (mod.ShowActionMenu)
                    hb.Append($@"
{await YetaWFCoreRendering.Render.RenderModuleLinksAsync(mod, ModuleAction.RenderModeEnum.NormalLinks, Globals.CssModuleLinksContainer)}");
            }

            hb.Append($@"
</div>");

        }

        private async Task RenderPanelModuleAsync(ModuleDefinition mod, HtmlBuilder hb, string htmlContents, string css, bool showTitle = true) {

            await Manager.AddOnManager.AddAddOnNamedAsync(AreaRegistration.CurrentPackage.AreaName, "PanelModule");

            string expCss = " t_expanded";
            if (mod.CanMinimize) {
                bool expanded = Manager.SessionSettings.GetModuleSettings(mod.ModuleGuid).GetValue<bool>("PanelExpanded", !mod.Minimized);
                if (!expanded)
                    expCss = " t_collapsed";
            }

            hb.Append($@"
<div id='{mod.ModuleHtmlId}' data-moduleguid='{mod.ModuleGuid.ToString()}' class='{css}{expCss}'>");

            if (!Manager.IsInPopup && mod.ShowModuleMenu) {
                hb.Append(await YetaWFCoreRendering.Render.RenderModuleMenuAsync(mod));
            }

            string actions = await YetaWFCoreRendering.Render.RenderModuleLinksAsync(mod, ModuleAction.RenderModeEnum.Button, Globals.CssModuleLinksContainer);
            if (string.IsNullOrWhiteSpace(actions))
                actions = "<div class='yModuleLinksContainer'></div>"; // empty div for flex

            hb.Append($@"
    <div class='yModuleTitle'>
         <h1>{Utility.HE(mod.Title)}</h1>
         {actions}");

            if (mod.CanMinimize) {
                
                hb.Append($@"
    <div class='yModuleExpColl'>
        <button class='y_buttonlite t_exp' {Basics.CssTooltip}='{Utility.HAE(this.__ResStr("exp", "Click to expand this panel"))}'>
            {SkinSVGs.Get(AreaRegistration.CurrentPackage, "fas-window-maximize")}
        </button>
        <button class='y_buttonlite t_coll' {Basics.CssTooltip}='{Utility.HAE(this.__ResStr("coll", "Click to collapse this panel"))}'>
            {SkinSVGs.Get(AreaRegistration.CurrentPackage, "fas-window-minimize")}
        </button>
    </div>");
            }

            hb.Append($@"
    </div>
    <div class='y_cleardiv'></div> 
    <div class='yModuleContents'>
{htmlContents}
    </div>
</div>");
        }

        /// <summary>
        /// Get all skin collections
        /// </summary>
        /// <returns></returns>
        public SkinCollectionInfoList GetAllSkinCollections() {
            if (_skinCollections == null) {
                List<Package.AddOnProduct> addonSkinColls = Package.GetAvailableSkinCollections();
                SkinCollectionInfoList newList = new SkinCollectionInfoList();
                foreach (Package.AddOnProduct addon in addonSkinColls) {
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
            return (from c in Package.GetAvailableSkinCollections() where c.SkinInfo.Name == collection select c.SkinInfo).FirstOrDefault();
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
        public PageSkinList GetAllPageSkins(string? skinCollection) {
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
        public PageSkinList GetAllPopupSkins(string? skinCollection) {
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
            Package.AddOnProduct addon = Package.FindSkin(skinCollection);
            return Popup ? addon.SkinInfo.PopupSkins : addon.SkinInfo.PageSkins;
        }

        /// <summary>
        /// Get all module skins for a collection
        /// </summary>
        /// <param name="skinCollection"></param>
        /// <returns>A list of module skins.</returns>
        public ModuleSkinList GetAllModuleSkins(string skinCollection) {
            Package.AddOnProduct addon = Package.FindSkin(skinCollection);
            return addon.SkinInfo.ModuleSkins;
        }

        /// <summary>
        /// Get all available themes for the current skin.
        /// </summary>
        /// <returns>A list of themes.</returns>
        public async Task<List<string>> GetThemesAsync() {
            List<string> list = new List<string>();
            SkinCollectionInfo skinInfo = FindSkinCollection(Manager.CurrentSite.Skin.Collection);
            foreach (string themePath in await FileSystem.FileSystemProvider.GetFilesAsync(Path.Combine(skinInfo.Folder, "Themes"), "*.css")) {
                string fileName = Path.GetFileNameWithoutExtension(themePath);
                if (fileName.EndsWith(".min", StringComparison.Ordinal))
                    continue;
                list.Add(fileName);
            }
            return list;
        }
    }
}
