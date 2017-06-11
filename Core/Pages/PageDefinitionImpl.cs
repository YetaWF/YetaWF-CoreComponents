/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using YetaWF.Core.Controllers;
using YetaWF.Core.Extensions;
using YetaWF.Core.Identity;
using YetaWF.Core.Image;
using YetaWF.Core.Models;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Modules;
using YetaWF.Core.Serializers;
using YetaWF.Core.Skins;
using YetaWF.Core.Support;
using YetaWF.Core.ResponseFilter;
#if MVC6
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
#else
using System.Web;
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Pages {

    public partial class PageDefinition : IInitializeApplicationStartup {

        protected YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        // IInitializeApplicationStartup
        public const string ImageType = "YetaWF_Core_PageFavIcon";

        public void InitializeApplicationStartup() {
            ImageSupport.AddHandler(ImageType, GetBytes: RetrieveImage);
        }
        private bool RetrieveImage(string name, string location, out byte[] content) {
            content = null;
            if (!string.IsNullOrWhiteSpace(location)) return false;
            if (string.IsNullOrWhiteSpace(name)) return false;
            PageDefinition page = PageDefinition.Load(new Guid(name));
            if (page == null) return false;
            if (page.FavIcon_Data == null || page.FavIcon_Data.Length == 0) return false;
            content = page.FavIcon_Data;
            return true;
        }

        public class DesignedPage {
            public string Url { get; set; } // absolute Url (w/o http: or domain) e.g., /Home or /Test/Page123
            public Guid PageGuid { get; set; }
        }
        public class UnifiedInfo {
            public Guid UnifiedSetGuid { get; set; }
            public UnifiedModeEnum Mode { get; set; }
            public string PageSkinCollectionName { get; set; }
            public string PageSkinFileName { get; set; }
            public int Animation { get; set; }
            public Guid MasterPageGuid { get; set; }
            public List<Guid> PageGuids { get; set; }
        }

        // this must be provided during app startup by a package implementing a page data provider
        public static Func<string, PageDefinition> CreatePageDefinition { get; set; }
        public static Func<Guid, PageDefinition> LoadPageDefinition { get; set; }
        public static Func<string, PageDefinition> LoadPageDefinitionByUrl { get; set; }
        public static Action<PageDefinition> SavePageDefinition { get; set; }
        public static Func<Guid, bool> RemovePageDefinition { get; set; }
        public static Func<List<DesignedPage>> GetDesignedPages { get; set; }
        public static Func<List<Guid>> GetDesignedGuids { get; set; }
        public static Func<List<string>> GetDesignedUrls { get; set; }
        public static Func<Guid, List<PageDefinition>> GetPagesFromModule { get; set; }

        public static Func<Guid?, string, string, UnifiedInfo> GetUnifiedPageInfo { get; set; }

        // When adding new properties, make sure to update EditablePage in PageEditModule so we can actually edit/view the property
        // When adding new properties, make sure to update EditablePage in PageEditModule so we can actually edit/view the property
        // When adding new properties, make sure to update EditablePage in PageEditModule so we can actually edit/view the property

        // PAGE MODULES
        // PAGE MODULES
        // PAGE MODULES

        public class ModuleEntry {
            [StringLength(PageDefinition.MaxPane)]
            public string Pane { get; set; }// if blank, use first pane
            public Guid ModuleGuid { get; set; }
            [DontSave]
            public ModuleDefinition Module { get { return GetModule(); } set { _module = value; } }// we're caching module definition here

            private ModuleDefinition GetModule()
            {
                if (_module == null)
                    _module = ModuleDefinition.Load(ModuleGuid);
                return _module;
            }
            ModuleDefinition _module = null;
        }

        public class ModuleList : SerializableList<ModuleEntry> {

            public void Remove(string pane, Guid moduleGuid, int indexInPane) {
                this.RemoveAll(m => (m.Pane == pane && m.ModuleGuid == moduleGuid && indexInPane == IndexInPane(moduleGuid, pane)));
            }
            private void RemoveAllModulesInPane(string pane) {
                this.RemoveAll(m => (m.Pane == pane));
            }

            public void MoveUp(string pane, Guid moduleGuid, int indexInPane) {
                ModuleList modsInPane = GetModulesForPane(pane);
                if (indexInPane <= 0 || indexInPane >= modsInPane.Count) throw new InternalError("Moving a module up - index in pane out of bounds - {0}", indexInPane);
                ModuleEntry me = modsInPane[indexInPane];
                modsInPane.RemoveAt(indexInPane);
                modsInPane.Insert(indexInPane - 1, me);
                RemoveAllModulesInPane(pane);
                this.AddRange(modsInPane);
            }
            public void MoveDown(string pane, Guid moduleGuid, int indexInPane) {
                ModuleList modsInPane = GetModulesForPane(pane);
                if (indexInPane < 0 || indexInPane >= modsInPane.Count-1) throw new InternalError("Moving a module down - index in pane out of bounds - {0}", indexInPane);
                ModuleEntry me = modsInPane[indexInPane];
                modsInPane.RemoveAt(indexInPane);
                modsInPane.Insert(indexInPane+1, me);
                RemoveAllModulesInPane(pane);
                this.AddRange(modsInPane);
            }
            public void MoveTop(string pane, Guid moduleGuid, int indexInPane) {
                ModuleList modsInPane = GetModulesForPane(pane);
                if (indexInPane < 0 || indexInPane >= modsInPane.Count) throw new InternalError("Moving a module to top - index in pane out of bounds - {0}", indexInPane);
                if (indexInPane == 0) return;
                ModuleEntry me = modsInPane[indexInPane];
                modsInPane.RemoveAt(indexInPane);
                modsInPane.Insert(0, me);
                RemoveAllModulesInPane(pane);
                this.AddRange(modsInPane);
            }
            public void MoveBottom(string pane, Guid moduleGuid, int indexInPane) {
                ModuleList modsInPane = GetModulesForPane(pane);
                if (indexInPane < 0 || indexInPane >= modsInPane.Count) throw new InternalError("Moving a module to top - index in pane out of bounds - {0}", indexInPane);
                if (indexInPane >= modsInPane.Count - 1) return;
                ModuleEntry me = modsInPane[indexInPane];
                modsInPane.RemoveAt(indexInPane);
                modsInPane.Add(me);
                RemoveAllModulesInPane(pane);
                this.AddRange(modsInPane);
            }

            public void MoveToPane(string oldPane, Guid moduleGuid, string newPane) {
                ModuleList modsInOldPane = GetModulesForPane(oldPane);
                ModuleList modsInNewPane = GetModulesForPane(newPane);
                int indexInPane = IndexInPane(moduleGuid, oldPane);
                ModuleEntry me = modsInOldPane[indexInPane];
                modsInOldPane.RemoveAt(indexInPane);
                me.Pane = newPane;
                modsInNewPane.Add(me);
                RemoveAllModulesInPane(oldPane);
                RemoveAllModulesInPane(newPane);
                this.AddRange(modsInOldPane);
                this.AddRange(modsInNewPane);
            }

            public int IndexInPane(ModuleDefinition mod, string pane) {
                return IndexInPane(mod.ModuleGuid, pane);
            }
            public int IndexInPane(Guid moduleGuid, string pane) {
                int indexInPane = 0;
                for (int index = 0 ; index < this.Count ; ++index) {
                    if (pane == this[index].Pane) {
                        if (this[index].ModuleGuid == moduleGuid)
                            return indexInPane;
                        ++indexInPane;
                    }
                }
                throw new InternalError("IndexOf couldn't find module {0} in the list of modules for pane {1}", moduleGuid.ToString(), pane);
            }

            public ModuleList GetModulesForPane(string pane) {
                ModuleList list = new ModuleList();
                list.AddRange(from me in this where me.Pane == pane select me);
                return list;
            }
        }

        /// <summary>
        /// The modules on this page
        /// </summary>
        public ModuleList ModuleDefinitions { get; set; }

        public void AddModule(string pane, ModuleDefinition module, bool top = false) {
            Debug.Assert(!string.IsNullOrWhiteSpace(pane));
            PageDefinition.ModuleEntry modEntry = new PageDefinition.ModuleEntry {
                Pane = pane,
                ModuleGuid = module.ModuleGuid,
                Module = module,
            };
            if (top)
                ModuleDefinitions.Insert(0, modEntry);
            else
                ModuleDefinitions.Add(modEntry);
        }

        //public void RemoveModule(Guid moduleGuid, string pane) {
        //    foreach (var entry in ModuleDefinitions) {
        //        if (moduleGuid == entry.ModuleGuid && string.Compare(entry.Pane, pane, true) == 0)
        //            ModuleDefinitions.Remove(entry);
        //    }
        //}

        // PAGE INFO
        // PAGE INFO
        // PAGE INFO

        /// <summary>
        /// Returns the page name encoded using its unique id.
        /// </summary>
        public string PageGuidName {
            get {
                return GetPageGuidName(PageGuid);
            }
        }
        private static string GetPageGuidName(Guid pageGuid) {
            return pageGuid.ToString();
        }

        // FIND
        // FIND
        // FIND

        // Returns the user defined Url (Url property) or if none has been defined, a system generated Url.
        public string PageUrl {
            get {
                if (!string.IsNullOrWhiteSpace(Url))
                    return Url;
                return Globals.PageUrl + PageGuidName;
            }
        }

        public string CanonicalUrlLink {
            get {
                string url = EvaluatedCanonicalUrl;
                return string.Format("<link rel=\"canonical\" href=\"{0}\">", url);
            }
        }

        // SKIN
        // SKIN
        // SKIN

        // Displays the panes defined by the page skin
        public List<string> Panes {
            get {
                if (_panes == null) {
                    SkinAccess skinAccess = new SkinAccess();
                    _panes = skinAccess.Panes(Manager.IsInPopup ? SelectedPopupSkin : SelectedSkin, Manager.IsInPopup);
                }
                return _panes;
            }
        }
        List<string> _panes;

        // LOAD/SAVE
        // LOAD/SAVE
        // LOAD/SAVE

        /// <summary>
        /// Loads a page definition.
        /// If the page doesn't exist, null is returned.
        /// </summary>
        public static PageDefinition Load(Guid pageGuid) {
            return PageDefinition.LoadPageDefinition(pageGuid);
        }
        /// <summary>
        /// Creates a new temporary page definition.
        /// </summary>
        public static PageDefinition Create() {
            return new PageDefinition();
        }
        /// <summary>
        /// Saves a page definition.
        /// </summary>
        public void Save() {
            if (Temporary)
                throw new InternalError("Temporary pages cannot be saved");
            foreach (var moduleEntry in ModuleDefinitions) {
                try {
                    moduleEntry.Module.Temporary = false;
                    moduleEntry.Module.Save();
                } catch (Exception) { }// this can fail when modules no longer exist
            }
            PageDefinition.SavePageDefinition(this);
        }

        /// <summary>
        /// Saves a page definition.
        /// </summary>
        /// <param name="title"></param>
        /// <param name="description"></param>
        /// <param name="url"></param>
        /// <param name="modelPage"></param>
        /// <param name="copyModules"></param>
        /// <param name="message"></param>
        public static PageDefinition CreateNewPage(MultiString title, MultiString description, string url, PageDefinition modelPage, bool copyModules, out string message) {
            try {
                PageDefinition page = PageDefinition.CreatePageDefinition(url);
                if (modelPage != null) {
                    page.AllowedRoles = modelPage.AllowedRoles;
                    page.AllowedUsers = modelPage.AllowedUsers;
                    page.Copyright = modelPage.Copyright;
                    page.CssClass = modelPage.CssClass;
                    page.Description = string.IsNullOrWhiteSpace(description) ? modelPage.Description : description;
                    page.jQueryUISkin = modelPage.jQueryUISkin;
                    page.KendoUISkin = modelPage.KendoUISkin;
                    page.Keywords = modelPage.Keywords;
                    page.ModuleDefinitions = new ModuleList();
                    if (copyModules)
                        page.ModuleDefinitions.AddRange(from m in modelPage.ModuleDefinitions select m);
                    page.PageSecurity = modelPage.PageSecurity;
                    page.ReferencedModules = modelPage.ReferencedModules;
                    page.RobotNoArchive = modelPage.RobotNoArchive;
                    page.RobotNoFollow = modelPage.RobotNoFollow;
                    page.RobotNoIndex = modelPage.RobotNoIndex;
                    page.RobotNoSnippet = modelPage.RobotNoSnippet;
                    page.SelectedPopupSkin = modelPage.SelectedPopupSkin;
                    page.SelectedSkin = modelPage.SelectedSkin;
                    page.SyntaxHighlighterSkin = modelPage.SyntaxHighlighterSkin;
                    page.Title = string.IsNullOrWhiteSpace(title) ? modelPage.Title : title;
                    page.WantSearch = modelPage.WantSearch;
                    page.TemplateGuid = modelPage.TemplateGuid;
                } else {
                    page.Title = title;
                    page.Description = description;
                }
                message = "";
                return page;
            } catch (Exception exc) {
                message = exc.Message;
                return null;
            }
        }

        /// <summary>
        /// Removes a page definition.
        /// </summary>
        /// <param name="pageGuid"></param>
        public static bool TryRemove(Guid pageGuid) {
            return PageDefinition.RemovePageDefinition(pageGuid);
        }

        /// <summary>
        /// Loads a page definition.
        /// If the page doesn't exist, null is returned.
        /// </summary>
        public static PageDefinition LoadFromUrl(string url) {
            if (!url.StartsWith("/")) throw new InternalError("Not a local Url");
            int index = url.IndexOf("?");
            if (index >= 0) url = url.Truncate(index);
            string newUrl = YetaWFManager.UrlDecodePath(url);
            string newQs;
            return GetPageUrlFromUrlWithSegments(newUrl, "", out newUrl, out newQs);
        }

        // We'll accept any URL that looks like this:
        // /...local.../segment/segment/segment/segment/segment/segment
        // as there could be keywords which need to xlated to &segment=segment?...
        // we'll check if there is a page by checking the longest sequence of
        // segments, removing a pair at a time, until we find a page
        public static PageDefinition GetPageUrlFromUrlWithSegments(string url, string qs, out string newUrl, out string newQs) {
            if (!url.StartsWith("/")) throw new InternalError("Not a local Url");
            newUrl = url;
            newQs = qs;
            PageDefinition page = null;
            for ( ; ; ) {
                page = PageDefinition.LoadPageDefinitionByUrl(newUrl);
                if (page != null)
                    break;// found it

                string[] segs = newUrl.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                int seglen = segs.Length;
                if (seglen <= 2)
                    break;// didn't find anything that matched

                string val = segs[seglen - 1];
                string key = segs[seglen - 2];

                newUrl = "/" + string.Join("/", segs, 0, seglen - 2);
                newQs = newQs.AddQSSeparator();
                newQs += string.Format("{0}={1}", YetaWFManager.UrlEncodeArgs(key), YetaWFManager.UrlEncodeArgs(val));
            }
            return page;
        }

        public static void GetUrlFromUrlWithSegments(string url, string[] segments, int urlSegments, string origQuery, out string newUrl, out string newQs) {
            newUrl = url;
            newQs = origQuery;
            if (segments.Length > urlSegments) { // we have additional human readable args
                string[] args = (from s in segments select s.TrimEnd(new char[] { '/' })).ToArray();
                url = "";
                for (int i = 0 ; i < urlSegments ; ++i) {
                    url += args[i];
                    if (i < urlSegments - 1)
                        url += "/";
                }
                if (((args.Length - urlSegments) % 2) == 0) { // we need key/value pairs
                    for (int i = urlSegments ; i < args.Length ; i += 2) {
                        newQs = newQs.AddQSSeparator();
                        newQs += string.Format("{0}={1}", args[i], args[i + 1]);
                    }
                    newUrl = url;
                    return;
                }
            }
        }
        public static bool IsSamePage(string url1, string url2) {
            if (!url1.StartsWith("/")) {
                Uri uri = new Uri(url1);
                url1 = uri.LocalPath;
            }
            if (!url2.StartsWith("/")) {
                Uri uri = new Uri(url2);
                url2 = uri.LocalPath;
            }
            return (url1 == url2);
        }

        // RENDERING
        // RENDERING
        // RENDERING

#if MVC6
        public HtmlString RenderPane(IHtmlHelper<object> htmlHelper, string pane, string cssClass = null, bool Conditional = true, PageDefinition UnifiedMainPage = null, bool PaneDiv = true) {
#else
        public HtmlString RenderPane(HtmlHelper<object> htmlHelper, string pane, string cssClass = null, bool Conditional = true, PageDefinition UnifiedMainPage = null, bool PaneDiv = true) {
#endif
            pane = string.IsNullOrEmpty(pane) ? Globals.MainPane : pane;
            Manager.PaneRendered = pane;
            // copy page's moduleDefinitions
            List<ModuleEntry> moduleList = (from m in ModuleDefinitions select m).ToList();
            // add templatepage moduleDefinitions
            if (!Manager.EditMode && TemplatePage != null)
                moduleList.AddRange(TemplatePage.ModuleDefinitions);

            // render all modules that are on this pane
            StringBuilder sb = new StringBuilder();

            bool empty = true;
            if (Manager.EditMode && !Manager.IsInPopup && !Manager.CurrentPage.Temporary) { // add the pane name in edit mode
                if (UnifiedMainPage == null || UnifiedMainPage.Url == Manager.CurrentPage.Url) { // but only for main page
                    TagBuilder tagDiv = new TagBuilder("div");
                    tagDiv.AddCssClass(Manager.AddOnManager.CheckInvokedCssModule(Globals.CssPaneTag));
                    tagDiv.SetInnerText(pane);
                    sb = new StringBuilder(tagDiv.ToString(TagRenderMode.Normal));
                }
            }

            foreach (var modEntry in moduleList) {
                if (string.Compare(modEntry.Pane, pane, true) == 0) {
                    empty = false;
                    ModuleDefinition module = null;
                    try {
                        module = modEntry.Module;
                        if (module != null && module.IsAuthorized(ModuleDefinition.RoleDefinition.View))
                            sb.Append(module.RenderModule(htmlHelper).ToString());
                    } catch (Exception exc) {
                        sb.Append(ModuleDefinition.ProcessModuleError(exc, modEntry.ModuleGuid.ToString()).ToString());
                        ModuleDefinition modServices = ModuleDefinition.Load(Manager.CurrentSite.ModuleControlServices, AllowNone: true);
                        if (modServices != null) {
                            ModuleAction action = modServices.GetModuleAction("Remove", Manager.CurrentPage, null, modEntry.ModuleGuid, pane);
                            if (action != null) {
                                HtmlString act = action.Render(ModuleAction.RenderModeEnum.NormalLinks);
                                if (act != HtmlStringExtender.Empty) { // only render if the action actually is available
                                    sb.AppendFormat("<ul class='{0}'>", Globals.CssModuleLinks);
                                    sb.Append("<li>");
                                    sb.Append(act);
                                    sb.Append("</li>");
                                    sb.Append("</ul>");
                                }
                            }
                        }
                    }
                }
            }

            // generate pane if requested
            bool generate = PaneDiv;
            if (generate) {
                bool hide = false;
                if (Manager.EditMode) {
                    // all panes are always generated in edit mode
                    if (UnifiedMainPage != null && UnifiedMainPage.Url != Manager.CurrentPage.Url) // but only for main page
                        generate = false;
                } else if (empty && Conditional) {
                    // this pane is empty and the pane is marked as conditional, meaning don't generate if empty
                    generate = false;
                    if (UnifiedMainPage != null && UnifiedMainPage.Url == Manager.CurrentPage.Url) {
                        // however, we're not generating the main page of a unified page set
                        // do we generate it but hide it
                        generate = true;
                        hide = true;
                    }
                }
                if (generate) {
                    TagBuilder tagDiv = new TagBuilder("div");
                    tagDiv.Attributes.Add("data-pane", YetaWFManager.HtmlAttributeEncode(pane));// add pane name
                    if (!string.IsNullOrWhiteSpace(cssClass))
                        tagDiv.AddCssClass(Manager.AddOnManager.CheckInvokedCssModule(string.Format("{0}", cssClass.Trim())));
                    tagDiv.AddCssClass(Manager.AddOnManager.CheckInvokedCssModule("yPane"));
                    if (Conditional)
                        tagDiv.Attributes.Add("data-conditional", "true");
                    if (UnifiedMainPage != null) {
                        tagDiv.Attributes.Add("data-url", YetaWFManager.UrlEncodePath(Manager.CurrentPage.Url));// add url to div so we can identify for which Url this pane is active
                        tagDiv.AddCssClass("yUnified");
                        if (Manager.UnifiedMode == PageDefinition.UnifiedModeEnum.HideDivs && UnifiedMainPage.Url != Manager.CurrentPage.Url)
                            hide = true;
                    }
                    if (hide)
                        tagDiv.Attributes.Add("style", "display:none");
                    tagDiv.SetInnerHtml(sb.ToString());
                    sb = new StringBuilder(tagDiv.ToString(TagRenderMode.Normal));
                }
            }

            Manager.PaneRendered = null;

            // figure out which modules are not in a defined pane on this page and add these panes dynamically after the Main pane
            // don't consider template page for modules
            if (PaneDiv && pane == Globals.MainPane) {
                List<string> leftOver = (from m in ModuleDefinitions select m.Pane).Distinct().ToList();
                leftOver = (from l in leftOver where !Manager.CurrentPage.Panes.Contains(l) select l).ToList();
                // now render what's left
                foreach (string p in leftOver) {
                    sb.Append(RenderPane(htmlHelper, p, "yGeneratedPane"));
                }
            }
            return new HtmlString(sb.ToString());
        }

        public class PaneSet : IDisposable {
            protected YetaWFManager Manager { get { return YetaWFManager.Manager; } }
#if MVC6
            public PaneSet(IHtmlHelper<object> Html, bool conditional, bool sameHeight) {
#else
            public PaneSet(HtmlHelper<object> Html, bool conditional, bool sameHeight) {
#endif
                this.Html = Html;
                DivTag = new TagBuilder("div");
                Id = Manager.UniqueId();
                DivTag.Attributes.Add("id", Id);
                Conditional = conditional;
                SameHeight = sameHeight;
                DisposableTracker.AddObject(this);
            }
            public void Dispose() { Dispose(true); }
            protected virtual void Dispose(bool disposing) {
                if (disposing) DisposableTracker.RemoveObject(this);
                HtmlBuilder hb = new HtmlBuilder();
                hb.Append("<div class='y_cleardiv'></div>");
                hb.Append(DivTag.ToString(TagRenderMode.EndTag));
                hb.Append("<script>");
                hb.Append("YetaWF_Basics.showPaneSet('{0}', {1}, {2});", Id, Manager.EditMode ? 1 : 0, SameHeight ? 1 : 0);
                hb.Append("</script>");
                Html.ViewContext.Writer.Write(hb.ToString());
            }
            //~PaneSet() { Dispose(false); }

#if MVC6
            public IHtmlHelper<object> Html { get; private set; }
#else
            public HtmlHelper<object> Html { get; private set; }
#endif
            public TagBuilder DivTag { get; private set; }
            public string Id { get; private set; }
            public bool Conditional { get; private set; }
            public bool SameHeight { get; private set; }
        }
        /// <summary>
        /// Render a set of panes. If all panes are empty, the panes will be hidden (display:none) but still sent to the client
        /// in case we may want to manipulate the panes on the client side
        /// </summary>
#if MVC6
        public PaneSet RenderPaneSet(IHtmlHelper<object> htmlHelper, string cssClass = null, bool Conditional = true, bool SameHeight = true) {
#else
        public PaneSet RenderPaneSet(HtmlHelper<object> htmlHelper, string cssClass = null, bool Conditional = true, bool SameHeight = true) {
#endif
            PaneSet set = new PaneSet(htmlHelper, Conditional, SameHeight);
            if (!string.IsNullOrWhiteSpace(cssClass))
                set.DivTag.AddCssClass(Manager.AddOnManager.CheckInvokedCssModule(cssClass));
            set.DivTag.Attributes.Add("style","display:none");
            htmlHelper.ViewContext.Writer.Write(set.DivTag.ToString(TagRenderMode.StartTag));
            return set;
        }

        /// <summary>
        /// Render pane contents so they can be returned to the client (used during unified page sets dynamic module processing).
        /// </summary>
#if MVC6
        public void RenderPaneContents(IHtmlHelper<object> htmlHelper, PageContentController.DataIn dataIn, PageContentController.PageContentData model)
#else
        public void RenderPaneContents(HtmlHelper<object> htmlHelper, PageContentController.DataIn dataIn, PageContentController.PageContentData model)
#endif
        {
            if (dataIn.Panes == null) throw new InternalError("No panes with Unified=true found in current skin");
            foreach (string pane in dataIn.Panes) {

                string paneHtml = RenderPane(htmlHelper, pane, UnifiedMainPage: Manager.CurrentPage, PaneDiv: false).ToString();
                PageProcessing pageProc = new PageProcessing(Manager);
                paneHtml = pageProc.PostProcessContentHtml(paneHtml);
                if (!string.IsNullOrWhiteSpace(paneHtml)) {
                    if (!Manager.CurrentSite.DEBUGMODE && Manager.CurrentSite.Compression)
                        paneHtml = WhiteSpaceResponseFilter.Compress(Manager, paneHtml);
                    model.Content.Add(new Controllers.PageContentController.PaneContent {
                        Pane = pane,
                        HTML = paneHtml,
                    });
                }
            }
            model.Addons = htmlHelper.RenderUniqueModuleAddOns(ExcludedGuids: dataIn.UnifiedAddonMods).ToString();

            // clear any http errors that may have occurred if a module failed (otherwise our ajax request will fail)
            Manager.CurrentResponse.StatusCode = 200;
        }

        // AUTHORIZATION
        // AUTHORIZATION
        // AUTHORIZATION

        private bool IsAuthorized(Func<AllowedRole, AllowedEnum> testRole, Func<AllowedUser, AllowedEnum> testUser) {

            if (Resource.ResourceAccess.IsBackDoorWideOpen()) return true;

            // check if it's a superuser
            if (Manager.HasSuperUserRole)
                return true;

            if (Manager.HaveUser) {
                // we have a logged on user
                if (Manager.Need2FA) {
                    return IsAuthorized_Role(testRole, Resource.ResourceAccess.GetAnonymousRoleId()) || IsAuthorized_Role(testRole, Resource.ResourceAccess.GetUser2FARoleId());
                } else {
                    int superuserRole = Resource.ResourceAccess.GetSuperuserRoleId();
                    if (Manager.UserRoles != null && Manager.UserRoles.Contains(superuserRole))
                        return true;
                    // see if the user has a role that is explicitly forbidden to access this page
                    int userRole = Resource.ResourceAccess.GetUserRoleId();
                    foreach (AllowedRole allowedRole in AllowedRoles) {
                        if (Manager.UserRoles != null && Manager.UserRoles.Contains(allowedRole.RoleId)) {
                            if (testRole(allowedRole) == AllowedEnum.No)
                                return false;
                        }
                        if (allowedRole.RoleId == userRole) {// check if any logged on user is forbidden
                            if (testRole(allowedRole) == AllowedEnum.No)
                                return false;
                        }
                    }
                    // check if the user is explicitly forbidden
                    AllowedUser allowedUser = AllowedUser.Find(AllowedUsers, Manager.UserId);
                    if (allowedUser != null)
                        if (testUser(allowedUser) == AllowedEnum.No)
                            return false;
                    // see if the user has a role that is explicitly permitted to access this page
                    foreach (AllowedRole allowedRole in AllowedRoles) {
                        if (Manager.UserRoles != null && Manager.UserRoles.Contains(allowedRole.RoleId)) {
                            if (testRole(allowedRole) == AllowedEnum.Yes)
                                return true;
                        }
                        if (allowedRole.RoleId == userRole) {// check if any logged on user is permitted
                            if (testRole(allowedRole) == AllowedEnum.Yes)
                                return true;
                        }
                    }
                    // check if the user listed is explicitly allowed
                    if (allowedUser != null)
                        if (testUser(allowedUser) == AllowedEnum.Yes)
                            return true;
                }
            } else {
                return IsAuthorized_Role(testRole, Resource.ResourceAccess.GetAnonymousRoleId());
            }
            return false;
        }
        private bool IsAuthorized_Role(Func<AllowedRole, AllowedEnum> testRole, int role) {
            AllowedRole allowedRole = AllowedRole.Find(AllowedRoles, role);
            if (allowedRole != null) {
                // check if the role is explicitly forbidden
                if (testRole(allowedRole) == AllowedEnum.No)
                    return false;
                // check if the role is explicitly allowed
                if (testRole(allowedRole) == AllowedEnum.Yes)
                    return true;
            }
            return false;
        }

        public bool IsAuthorized_View() {
            return IsAuthorized((allowedRole) => allowedRole.View, (allowedUser) => allowedUser.View);
        }
        public bool IsAuthorized_Edit() {
            return IsAuthorized((allowedRole) => allowedRole.Edit, (allowedUser) => allowedUser.Edit);
        }
        public bool IsAuthorized_Remove() {
            return IsAuthorized((allowedRole) => allowedRole.Remove, (allowedUser) => allowedUser.Remove);
        }

        public bool IsAuthorized_View_Anonymous() {
            return IsAuthorized_Role((allowedRole) => allowedRole.View, Resource.ResourceAccess.GetAnonymousRoleId());
        }
        public bool IsAuthorized_View_AnyUser() {
            return IsAuthorized_Role((allowedRole) => allowedRole.View, Resource.ResourceAccess.GetUserRoleId());
        }

        // FAVICON
        // FAVICON
        // FAVICON

        public string FavIconLink {
            get {
                return Manager.CurrentSite.GetFavIconLinks(FavIcon_Data, FavIcon, null, null);// no support for large icons
            }
        }

        // TEMPLATE
        // TEMPLATE
        // TEMPLATE

        public PageDefinition TemplatePage {
            get {
                if (!_templatePageEvaluated) {
                    _templatePageEvaluated = true;
                    if (TemplateGuid != null)
                        _templatePage = PageDefinition.Load((Guid)TemplateGuid);
                }
                return _templatePage;
            }
        }
        private PageDefinition _templatePage = null;
        private bool _templatePageEvaluated = false;

        // HREFLANG
        // HREFLANG
        // HREFLANG

        /// <summary>
        /// Returns the page's language id. If none is defined, the default language is returned.
        /// </summary>
        /// <returns>Returns the page's language id.</returns>
        public string GetPageLanguageId() {
            string pageLanguage = Manager.UserLanguage;
            if (string.IsNullOrWhiteSpace(pageLanguage))
                pageLanguage = LanguageId;// page language
            if (string.IsNullOrWhiteSpace(pageLanguage))
                pageLanguage = MultiString.DefaultLanguage;
            return pageLanguage;
        }

        /// <summary>
        /// Returns all html needed to defined the page's language.
        /// </summary>
        public string HrefLangHtml {
            get {
                return GetHrefLangHtml();
            }
        }

        /// <summary>
        /// Build html to define hreflang and metadata for all available languages.
        /// </summary>
        /// <param name="page">PageDefinition</param>
        /// <returns>All Html needed to define the page's available languages using hreflang tags and meta tags.</returns>
        private string GetHrefLangHtml() {

            HtmlBuilder hb = new HtmlBuilder();

            string pageLanguage = Manager.UserLanguage;
            if (string.IsNullOrWhiteSpace(pageLanguage))
                pageLanguage = LanguageId;// page language
            if (string.IsNullOrWhiteSpace(pageLanguage))
                pageLanguage = MultiString.DefaultLanguage;

            // hreflang - google

            // <link rel="alternate" href="http://example.com/" hreflang = "x-default" />
            string canonUrl = YetaWFManager.HtmlAttributeEncode(EvaluatedCanonicalUrl);
            hb.Append("<link rel='alternate' href='{0}' hreflang='x-default' />", canonUrl);
            if (string.IsNullOrWhiteSpace(LanguageId)) {
                // page in multiple languages
                // build all alternate pages with language specific url arg
                {
                    foreach (Language.LanguageData lang in MultiString.Languages) {
                        hb.Append(GetLanguageUrl(canonUrl, lang.Id));
                    }
                }
            } else {
                // Single language page
                hb.Append("<link rel='alternate' href='{0}' hreflang='{1}' />", canonUrl, pageLanguage);
            }

            // meta - bing
            // not used as it's obsolete in html5
            //hb.Append("<meta http-equiv='content-language' content='{0}' />", pageLanguage);
            return hb.ToString();
        }

        private string GetLanguageUrl(string canonUrl, string pageLanguage) {
            string sep = canonUrl.Contains('?') ? "&amp;" : "?";
            if (pageLanguage == MultiString.DefaultLanguage)
                return string.Format("<link rel='alternate' href='{0}' hreflang='{1}' />", canonUrl, pageLanguage);
            else
                return string.Format("<link rel='alternate' href='{0}{1}{2}={3}' hreflang='{4}' />", canonUrl, sep, Globals.Link_Language, pageLanguage, pageLanguage);
        }

        // When adding new properties, make sure to update EditablePage in PageEditModule so we can actually edit/view the property
        // When adding new properties, make sure to update EditablePage in PageEditModule so we can actually edit/view the property
        // When adding new properties, make sure to update EditablePage in PageEditModule so we can actually edit/view the property
    }
}
