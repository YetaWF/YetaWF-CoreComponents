/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using YetaWF.Core.Addons;
using YetaWF.Core.IO;
using YetaWF.Core.Localize;
using YetaWF.Core.Support;
#if MVC6
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
#else
using System.Web;
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Modules {
    public static class ModuleDefinitionExtensions {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(ModuleDefinitionExtensions), name, defaultValue, parms); }

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }


#if MVC6
        public static HtmlString RenderPageControl<TYPE>(this IHtmlHelper<object> htmlHelper) {
#else
        public static HtmlString RenderPageControl<TYPE>(this HtmlHelper<object> htmlHelper) {
#endif
            Guid permGuid = ModuleDefinition.GetPermanentGuid(typeof(TYPE));
            return htmlHelper.RenderPageControl(permGuid);
        }

#if MVC6
        private static HtmlString RenderPageControl(this IHtmlHelper<object> htmlHelper, Guid moduleGuid) {
#else
        private static HtmlString RenderPageControl(this HtmlHelper<object> htmlHelper, Guid moduleGuid) {
#endif
            if (Manager.IsInPopup) return HtmlStringExtender.Empty;
            if (Manager.CurrentPage == null || Manager.CurrentPage.Temporary) return HtmlStringExtender.Empty;

            if (Manager.CurrentPage.IsAuthorized_Edit()) {

                ModuleDefinition mod = ModuleDefinition.Load(moduleGuid);

                ModuleAction action = new ModuleAction(mod) {
                    Category = ModuleAction.ActionCategoryEnum.Significant,
                    CssClass = "",
                    Image = "PageEdit.png",
                    Location = ModuleAction.ActionLocationEnum.Any,
                    Mode = ModuleAction.ActionModeEnum.Any,
                    Style = ModuleAction.ActionStyleEnum.Nothing,
                    LinkText = __ResStr("pageControlLink", "Control Panel"),
                    MenuText = __ResStr("pageControlMenu", "Control Panel"),
                    Tooltip = __ResStr("pageControlTT", "Control Panel - Add new or existing modules, add new pages, switch to edit mode and access page settings"),
                    Legend = __ResStr("pageControlLeg", "Control Panel - Adds new or existing modules, adds new pages, switches to edit mode and accesses page settings"),
                };

                // <div class=CssPageControlDiv>
                //  action button (with id CssPageControlButton)
                //  module html...
                // </div>
                TagBuilder tag = new TagBuilder("div");
                tag.Attributes.Add("id", Globals.CssPageControlDiv);

                Manager.ForceModuleActionLinks = true; // so we get module action links (we're not in a pane)
                tag.SetInnerHtml(action.RenderAsButtonIcon(Globals.CssPageControlButton).ToString() + mod.RenderModule(htmlHelper).ToString());
                Manager.ForceModuleActionLinks = false;
                return tag.ToHtmlString(TagRenderMode.Normal);
            } else
                return HtmlStringExtender.Empty;
        }

#if MVC6
        public static HtmlString RenderEditControl<TYPE>(this IHtmlHelper<object> htmlHelper) {
#else
        public static HtmlString RenderEditControl<TYPE>(this HtmlHelper<object> htmlHelper) {
#endif
            Guid permGuid = ModuleDefinition.GetPermanentGuid(typeof(TYPE));
            return htmlHelper.RenderEditControl(permGuid);
        }

#if MVC6
        private static HtmlString RenderEditControl(this IHtmlHelper<object> htmlHelper, Guid moduleGuid) {
#else
        private static HtmlString RenderEditControl(this HtmlHelper<object> htmlHelper, Guid moduleGuid) {
#endif
            if (Manager.IsInPopup) return HtmlStringExtender.Empty;
            //if (Manager.CurrentPage == null || Manager.CurrentPage.Temporary) return HtmlStringExtender.Empty;

            if (Manager.CurrentPage.IsAuthorized_Edit()) {

                ModuleDefinition mod = ModuleDefinition.Load(moduleGuid);

                // <div class=CssEditControlDiv>
                //  action button (with id CssEditControlButton)
                // </div>
                TagBuilder tag = new TagBuilder("div");
                tag.Attributes.Add("id", Globals.CssEditControlDiv);


                ModuleAction action = mod.GetModuleAction(Manager.EditMode ? "SwitchToView" : "SwitchToEdit");
                if (Manager.EditMode) {
                    action.LinkText = __ResStr("editControlLinkToEdit", "Switch to Edit Mode");
                    action.MenuText = __ResStr("editControlLinkToEdit", "Switch to Edit Mode");
                } else {
                    action.LinkText = __ResStr("editControlLinkToView", "Switch to View Mode");
                    action.MenuText = __ResStr("editControlLinkToView", "Switch to View Mode");
                }
                tag.SetInnerHtml(action.RenderAsButtonIcon(Globals.CssEditControlButton).ToString() + mod.RenderModule(htmlHelper).ToString());// mainly just to get js/css, the module is normally empty

                return tag.ToHtmlString(TagRenderMode.Normal);
            } else
                return HtmlStringExtender.Empty;
        }

        // unique, possibly new, typically used in skin to create unique non-pane modules
#if MVC6
        public static HtmlString RenderModule<TYPE>(this IHtmlHelper htmlHelper, Action<TYPE> initModule = null) {
#else
        public static HtmlString RenderModule<TYPE>(this HtmlHelper htmlHelper, Action<TYPE> initModule = null) {
#endif
            return htmlHelper.RenderUniqueModule(typeof(TYPE), (mod) => {
                if (initModule != null)
                    initModule((TYPE)mod);
            });
        }
#if MVC6
        public static HtmlString RenderUniqueModule(this IHtmlHelper htmlHelper, Type modType, Action<object> initModule = null) {
#else
        public static HtmlString RenderUniqueModule(this HtmlHelper htmlHelper, Type modType, Action<object> initModule = null) {
#endif
            Guid permGuid = ModuleDefinition.GetPermanentGuid(modType);
            ModuleDefinition mod = null;
            try {
                StringLocks.DoAction(permGuid.ToString(), () => {
                    mod = ModuleDefinition.LoadModuleDefinition(permGuid);
                    if (mod == null) {
                        mod = ModuleDefinition.CreateNewDesignedModule(permGuid, null, null);
                        if (!mod.IsModuleUnique)
                            throw new InternalError("{0} is not a unique module (must specify a module guid)", modType.FullName);
                        mod.ModuleGuid = permGuid;
                        mod.Temporary = false;
                        if (initModule != null)
                            initModule(mod);
                        mod.Save();
                    } else {
                        if (!mod.IsModuleUnique)
                            throw new InternalError("{0} is not a unique module (must specify a module guid)", modType.FullName);
                        mod.Temporary = false;
                    }
                });
            } catch (Exception exc) {
                HtmlBuilder hb = ModuleDefinition.ProcessModuleError(exc, permGuid.ToString());
                return hb.ToHtmlString();
            }
            return mod.RenderModule(htmlHelper);
        }
#if MVC6
        public static HtmlString RenderReferencedModule_Ajax(this IHtmlHelper htmlHelper, Type modType, Action<object> initModule = null) {
#else
        public static HtmlString RenderReferencedModule_Ajax(this HtmlHelper htmlHelper, Type modType, Action<object> initModule = null) {
#endif
            Guid permGuid = ModuleDefinition.GetPermanentGuid(modType);
            ModuleDefinition mod = null;
            try {
                StringLocks.DoAction(permGuid.ToString(), () => {
                    mod = ModuleDefinition.LoadModuleDefinition(permGuid);
                    if (mod == null) {
                        mod = ModuleDefinition.CreateNewDesignedModule(permGuid, null, null);
                        if (!mod.IsModuleUnique)
                            throw new InternalError("{0} is not a unique module (must specify a module guid)", modType.FullName);
                        mod.ModuleGuid = permGuid;
                        mod.Temporary = false;
                        if (initModule != null)
                            initModule(mod);
                        mod.Save();
                    } else {
                        if (!mod.IsModuleUnique)
                            throw new InternalError("{0} is not a unique module (must specify a module guid)", modType.FullName);
                        mod.Temporary = false;
                    }
                });
            } catch (Exception exc) {
                HtmlBuilder hb = ModuleDefinition.ProcessModuleError(exc, permGuid.ToString());
                return hb.ToHtmlString();
            }
            return mod.RenderReferencedModule_Ajax(htmlHelper);
        }

        // non-unique, possibly new, typically used in skin to create non-unique non-pane modules

#if MVC6
        public static HtmlString RenderModule<TYPE>(this IHtmlHelper<object> htmlHelper, Guid moduleGuid, Action<TYPE> initModule = null)
#else
        public static HtmlString RenderModule<TYPE>(this HtmlHelper<object> htmlHelper, Guid moduleGuid, Action<TYPE> initModule = null)
#endif
        {
            ModuleDefinition mod = null;
            try {
                StringLocks.DoAction(moduleGuid.ToString(), () => {
                    mod = ModuleDefinition.LoadModuleDefinition(moduleGuid);
                    if (mod == null) {
                        Guid permGuid = ModuleDefinition.GetPermanentGuid(typeof(TYPE));
                        mod = ModuleDefinition.CreateNewDesignedModule(permGuid, null, null);
                        if (mod.IsModuleUnique)
                            throw new InternalError("{0} is a unique module (can't specify a module guid)", typeof(TYPE).FullName);
                        mod.ModuleGuid = moduleGuid;
                        if (initModule != null)
                            initModule((TYPE)(object)mod);
                        mod.Temporary = false;
                        mod.Save();
                    } else {
                        if (mod.IsModuleUnique)
                            throw new InternalError("{0} is a unique module (can't specify a module guid)", typeof(TYPE).FullName);
                        mod.Temporary = false;
                    }
                });
            } catch (Exception exc) {
                HtmlBuilder hb = ModuleDefinition.ProcessModuleError(exc, moduleGuid.ToString());
                return hb.ToHtmlString();
            }
            return mod.RenderModule(htmlHelper);
        }

#if MVC6
        public static HtmlString RenderUniqueModuleAddOns(this IHtmlHelper htmlHelper) {
#else
        public static HtmlString RenderUniqueModuleAddOns(this HtmlHelper htmlHelper) {
#endif
            Manager.Verify_NotAjaxRequest();
            List<AddOnManager.Module> mods = Manager.AddOnManager.GetAddedUniqueInvokedCssModules();
            HtmlBuilder hb = new HtmlBuilder();
            Manager.RenderingUniqueModuleAddons = true;
            foreach (AddOnManager.Module mod in mods) {
                if (!Manager.IsInPopup || mod.AllowInPopup) {
                    hb.Append(htmlHelper.RenderUniqueModule(mod.ModuleType));
                }
            }
            Manager.RenderingUniqueModuleAddons = false;
            return hb.ToHtmlString();
        }
#if MVC6
        public static HtmlString RenderReferencedModule_Ajax(this IHtmlHelper htmlHelper) {
#else
        public static HtmlString RenderReferencedModule_Ajax(this HtmlHelper htmlHelper) {
#endif
            Manager.Verify_AjaxRequest();
            List<AddOnManager.Module> mods = Manager.AddOnManager.GetAddedUniqueInvokedCssModules();
            HtmlBuilder hb = new HtmlBuilder();
            Manager.RenderingUniqueModuleAddonsAjax = true;
            foreach (AddOnManager.Module mod in mods) {
                if (mod.AllowInAjax)
                    hb.Append(htmlHelper.RenderReferencedModule_Ajax(mod.ModuleType));
            }
            Manager.RenderingUniqueModuleAddonsAjax = false;
            return hb.ToHtmlString();
        }
    }
}
