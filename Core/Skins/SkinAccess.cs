/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using YetaWF.Core.Addons;
using YetaWF.Core.Identity;
using YetaWF.Core.Localize;
using YetaWF.Core.Modules;
using YetaWF.Core.Support;
#if MVC6
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
#else
using System.Web;
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Skins {

    public partial class SkinAccess {

        public const string FallbackSkinCollectionName = "YetaWF/Core/Standard";
        public const string FallbackPageFileName = "Default.cshtml";
        public const string FallbackPagePlainFileName = "Plain.cshtml";
        public const string FallbackPopupSkinCollectionName = "YetaWF/Core/Standard";
        public const string FallbackPopupFileName = "Popup.cshtml";
        public const string FallbackPopupMediumFileName = "PopupMedium.cshtml";
        public const string FallbackPopupSmallFileName = "PopupSmall.cshtml";
        public const string PageFolder = "Pages";
        public const string PopupFolder = "Popups";

        public SkinAccess() {  }

        protected YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        public string PhysicalPageUrl(SkinDefinition skin, bool popup) {
            skin = SkinDefinition.EvaluatedSkin(skin, popup);
            VersionManager.AddOnProduct addon = VersionManager.FindSkinVersion(ref skin, popup);
            if (string.IsNullOrWhiteSpace(skin.Collection)) {
                if (popup)
                    skin = Manager.CurrentSite.SelectedPopupSkin;
                else
                    skin = Manager.CurrentSite.SelectedSkin;
            }
            string collection = skin.Collection;
            if (string.IsNullOrWhiteSpace(collection)) {
                if (popup) {
                    collection = FallbackSkinCollectionName;
                } else {
                    collection = FallbackPopupSkinCollectionName;
                }
            }
            string fileName = skin.FileName;
            if (string.IsNullOrWhiteSpace(fileName)) {
                if (popup)
                    fileName = FallbackPopupFileName;
                else
                    fileName = FallbackPageFileName;
            }
            return addon.GetAddOnUrl() + (popup ? PopupFolder : PageFolder) +  "/" + fileName;
        }

        // get the local filename of the page skin
        public string PageSkinFile(SkinDefinition skin, bool popup) {
            return YetaWFManager.UrlToPhysical(PhysicalPageUrl(skin, popup));
        }

        // get the contents of the page skin file
        public string PageSkinFileContents(string filePath) {
            string contents = File.ReadAllText(filePath);
            contents = contents.Replace('\r', ' ').Replace('\n', ' ');
            return contents;
        }

        // Returns the panes defined by the page skin
        public List<string> Panes(SkinDefinition pageSkin, bool popup) {
            List<string> panes = new List<string>();
            string fileName = PageSkinFile(pageSkin, popup);
            string contents = PageSkinFileContents(fileName);
            Regex regex = new Regex(MakeRegexPattern(regexRazorRenderPane), RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant);
            Match m = regex.Match(contents);
            while (m.Success) {

                string content = m.Groups[1].Value;
                if (content == Globals.MainPane)
                    throw new InternalError("A pane cannot be named {0} as that is a reserved name.", Globals.MainPane);
                if (content == "")
                    content = Globals.MainPane;
                panes.Add(content);
                m = m.NextMatch();
            }
            if (panes.Count == 0)
                throw new InternalError("No panes defined in {0}", fileName);
            return panes;
        }
        private const string regexRazorRenderPane = "[\\@ ]RenderPane \\( \"([^\"]*)\"[^\\)]*\\)";

        private static string MakeRegexPattern(string strPatt) {
            strPatt = strPatt.Replace("  ", "\\s+");
            strPatt = strPatt.Replace(" ", "\\s*");
            return strPatt;
        }
        public void GetModuleCharacterSizes(ModuleDefinition mod, out int width, out int height) {
            ModuleSkinEntry modSkinEntry = GetModuleSkinEntry(mod);
            width = modSkinEntry.CharWidthAvg;
            height = modSkinEntry.CharHeight;
        }
        public SkinCollectionInfo GetSkinCollectionInfo() {
            SkinDefinition pageSkin = SkinDefinition.EvaluatedSkin(Manager.CurrentPage, Manager.IsInPopup);
            SkinCollectionInfo info = TryFindSkinCollection(pageSkin.Collection);
            if (info == null)
                info = FindSkinCollection(Manager.IsInPopup ? SkinAccess.FallbackPopupSkinCollectionName : SkinAccess.FallbackSkinCollectionName);
            return info;
        }
        public PageSkinEntry GetPageSkinEntry() {
            SkinDefinition pageSkin = SkinDefinition.EvaluatedSkin(Manager.CurrentPage, Manager.IsInPopup);
            SkinCollectionInfo info = TryFindSkinCollection(pageSkin.Collection);
            PageSkinEntry pageSkinEntry;
            if (info == null) {
                info = FindSkinCollection(Manager.IsInPopup ? SkinAccess.FallbackPopupSkinCollectionName : SkinAccess.FallbackSkinCollectionName);
                pageSkinEntry = Manager.IsInPopup ? info.PopupSkins.First() : info.PageSkins.First();
            } else {
                string fileName = pageSkin.FileName;
                if (string.IsNullOrWhiteSpace(fileName)) {
                    if (Manager.IsInPopup)
                        fileName = FallbackPopupFileName;
                    else
                        fileName = FallbackPageFileName;
                }
                if (Manager.IsInPopup) {
                    pageSkinEntry = (from s in info.PopupSkins where s.FileName == fileName select s).FirstOrDefault();
                } else {
                    pageSkinEntry = (from s in info.PageSkins where s.FileName == fileName select s).FirstOrDefault();
                }
            }
            if (pageSkinEntry == null)
                throw new InternalError("No page skin {0} found", pageSkin);
            return pageSkinEntry;
        }
        private ModuleSkinEntry GetModuleSkinEntry(ModuleDefinition mod) {
            // Get the skin info for the page's skin collection and get the default module skin
            SkinDefinition pageSkin = SkinDefinition.EvaluatedSkin(Manager.CurrentPage, Manager.IsInPopup);
            SkinCollectionInfo info = TryFindSkinCollection(pageSkin.Collection);
            ModuleSkinEntry modSkinEntry;
            if (info == null) {
                info = FindSkinCollection(Manager.IsInPopup ? SkinAccess.FallbackPopupSkinCollectionName : SkinAccess.FallbackSkinCollectionName);
                modSkinEntry = info.ModuleSkins.First();
            } else {
                // Find the module skin name to use for the page's skin collection
                string modSkin = (from s in mod.SkinDefinitions where s.Collection == pageSkin.Collection select s.FileName).FirstOrDefault();
                if (string.IsNullOrWhiteSpace(modSkin))
                    modSkinEntry = info.ModuleSkins.First();
                else
                    modSkinEntry = (from s in info.ModuleSkins where s.CssClass == modSkin select s).FirstOrDefault();
                if (modSkinEntry == null) throw new InternalError("No module skin {0} found", modSkin);
            }
            return modSkinEntry;
        }

        // a module looks like this - we build this dynamically
        // <div class='[Globals,CssModule] [ThisModule,Area] [ThisModule,CssClass]' id='[ThisModule,ModuleHtmlId]'
        //      data-moduleguid='[ThisModule,ModuleGuid]' data-charwidth='..' data-charheightavg='..' >
        //    [ThisModule,ModuleMenu]
        //    [ThisModule,TitleHtml]
        //    [[CONTENTS]]
        //    [ThisModule,ActionMenu]
        // </div>
        // Depending on the BootstrapContainer property <div class="container"> and <div class="row"> may be added.
        internal HtmlString MakeModuleContainer(ModuleDefinition mod, string htmlContents, bool ShowMenu = true, bool ShowTitle = true, bool ShowAction = true) {
            ModuleSkinEntry modSkinEntry = GetModuleSkinEntry(mod);
            string modSkinCss = modSkinEntry.CssClass;

            TagBuilder anchor = null;
            if (!mod.IsModuleUnique && Manager.CurrentPage != null && !string.IsNullOrWhiteSpace(mod.AnchorId)) { // add an anchor
                anchor = new TagBuilder("div");
                anchor.AddCssClass("yAnchor");
                anchor.Attributes.Add("id", mod.AnchorId);
            }

            TagBuilder div = new TagBuilder("div");
            div.AddCssClass(Manager.AddOnManager.CheckInvokedCssModule(Globals.CssModule));
            div.AddCssClass(Manager.AddOnManager.CheckInvokedCssModule(mod.Area));
            div.AddCssClass(Manager.AddOnManager.CheckInvokedCssModule(mod.Area + "_" + mod.ModuleName));
            div.AddCssClass(Manager.AddOnManager.CheckInvokedCssModule(modSkinCss));
            if (!string.IsNullOrWhiteSpace(mod.CssClass))
                div.AddCssClass(Manager.AddOnManager.CheckInvokedCssModule(mod.CssClass));
            if (!mod.Print)
                div.AddCssClass(Manager.AddOnManager.CheckInvokedCssModule(Globals.CssModuleNoPrint));
            div.Attributes.Add("id", mod.ModuleHtmlId);
            div.Attributes.Add("data-moduleguid", mod.ModuleGuid.ToString());
            div.Attributes.Add("data-charwidthavg", Manager.CharWidthAvg.ToString());
            div.Attributes.Add("data-charheight", Manager.CharHeight.ToString());

            HtmlBuilder inner = new HtmlBuilder();
            if (mod.BootstrapContainer == ModuleDefinition.BootstrapContainerEnum.ContainerRow)
                inner.Append("<div class='container'><div class='row'>");
            else if (mod.BootstrapContainer == ModuleDefinition.BootstrapContainerEnum.ContainerOnly)
                inner.Append("<div class='container'>");

            // add an inner div with css classes to modules that can't be seen by anonymous users and users
            bool showOwnership = UserSettings.GetProperty<bool>("ShowModuleOwnership") &&
                                    Resource.ResourceAccess.IsResourceAuthorized(CoreInfo.Resource_ViewOwnership);
            if (showOwnership) {
                bool anon = mod.IsAuthorized_View_Anonymous();
                bool user = mod.IsAuthorized_View_AnyUser();
                if (!anon && !user)
                    inner.Append("<div class='ymodrole_noUserAnon'>");
                else if (!anon)
                    inner.Append("<div class='ymodrole_noAnon'>");
                else if (!user)
                    inner.Append("<div class='ymodrole_noUser'>");
                else
                    showOwnership = false;
            }

            if (ShowMenu)
                inner.Append(mod.ModuleMenuHtml);
            if (ShowTitle) {
                if (mod.ShowTitleActions) {
                    string actions = mod.ActionTopMenuHtml;
                    if (!string.IsNullOrWhiteSpace(actions)) {
                        inner.Append("<div class='yModuleTitle'>");
                        inner.Append(mod.TitleHtml);
                        inner.Append(actions);
                        inner.Append("</div>");
                        inner.Append("<div class='y_cleardiv'></div>");
                    } else {
                        inner.Append(mod.TitleHtml);
                    }
                } else {
                    inner.Append(mod.TitleHtml);
                }
            }
            inner.Append(htmlContents);
            if (ShowAction && (!string.IsNullOrWhiteSpace(Manager.PaneRendered) || Manager.ForceModuleActionLinks)) // only show action menus in a pane
                inner.Append(mod.ActionMenuHtml);

            if (mod.BootstrapContainer == ModuleDefinition.BootstrapContainerEnum.ContainerRow)
                inner.Append("</div></div>");
            else if (mod.BootstrapContainer == ModuleDefinition.BootstrapContainerEnum.ContainerOnly)
                inner.Append("</div>");

            if (showOwnership)
                inner.Append("</div>");

            div.SetInnerHtml(inner.ToString());
            if (anchor != null)
                return new HtmlString(anchor.ToString(TagRenderMode.Normal) + div.ToString(TagRenderMode.Normal));
            else
                return div.ToHtmlString(TagRenderMode.Normal);
        }

        /// <summary>
        /// Get all skin collections
        /// </summary>
        /// <returns></returns>
        public SkinCollectionInfoList GetAllSkinCollections() {
            if (_skinCollections == null) {
                List<VersionManager.AddOnProduct> addonSkinColls = VersionManager.GetAvailableSkinCollections();
                _skinCollections = new SkinCollectionInfoList();
                foreach (VersionManager.AddOnProduct addon in addonSkinColls) {
                    _skinCollections.Add(addon.SkinInfo);
                }
            }
            return _skinCollections;
        }
        private static SkinCollectionInfoList _skinCollections;

        /// <summary>
        /// Find a skin collections
        /// </summary>
        /// <returns></returns>
        protected SkinCollectionInfo TryFindSkinCollection(string collection) {
            return (from c in VersionManager.GetAvailableSkinCollections() where c.SkinInfo.CollectionName == collection select c.SkinInfo).FirstOrDefault();
        }
        /// <summary>
        /// Find a skin collections
        /// </summary>
        /// <returns></returns>
        protected SkinCollectionInfo FindSkinCollection(string collection) {
            SkinCollectionInfo info = TryFindSkinCollection(collection);
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
                        FileName = FallbackPageFileName,
                    },
                    new PageSkinEntry {
                        Name = this.__ResStr("pagePlain", "Plain Page"),
                        Description = this.__ResStr("pagePlainTT", "Plain Page"),
                        FileName = FallbackPagePlainFileName,
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
                        FileName = FallbackPopupFileName,
                    },
                    new PageSkinEntry {
                        Name = this.__ResStr("popMed", "Medium Popup"),
                        Description = this.__ResStr("popMedTT", "Medium popup"),
                        FileName = FallbackPopupMediumFileName,
                    },
                    new PageSkinEntry {
                        Name = this.__ResStr("popSmall", "Small Popup"),
                        Description = this.__ResStr("popSmallTT", "Small popup"),
                        FileName = FallbackPopupSmallFileName,
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
    }
}
