/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using YetaWF.Core.Addons;
using YetaWF.Core.IO;
using YetaWF.Core.Localize;
using YetaWF.Core.Support;
using System.Linq;
using System.Threading.Tasks;
using YetaWF.Core.Packages;
using YetaWF.Core.Skins;
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
        public static async Task<HtmlString> RenderPageControlAsync<TYPE>(this IHtmlHelper<object> htmlHelper) {
#else
        public static async Task<HtmlString> RenderPageControlAsync<TYPE>(this HtmlHelper<object> htmlHelper) {
#endif
            Guid permGuid = ModuleDefinition.GetPermanentGuid(typeof(TYPE));
            return await htmlHelper.RenderPageControlAsync(permGuid);
        }

#if MVC6
        private static async Task<HtmlString> RenderPageControlAsync(this IHtmlHelper<object> htmlHelper, Guid moduleGuid) {
#else
        private static async Task<HtmlString> RenderPageControlAsync(this HtmlHelper<object> htmlHelper, Guid moduleGuid) {
#endif
            if (Manager.IsInPopup) return HtmlStringExtender.Empty;
            if (Manager.CurrentPage == null || Manager.CurrentPage.Temporary) return HtmlStringExtender.Empty;

#if DEBUG
            // allow in debug mode without checking unless marked deployed
            if (Manager.Deployed && !Manager.CurrentPage.IsAuthorized_Edit()) return HtmlStringExtender.Empty;
#else
            if (!Manager.CurrentPage.IsAuthorized_Edit()) return HtmlStringExtender.Empty;
#endif
            ModuleDefinition mod = await ModuleDefinition.LoadAsync(moduleGuid);

            ModuleAction action = new ModuleAction(mod) {
                Category = ModuleAction.ActionCategoryEnum.Significant,
                CssClass = "",
                Image = await new SkinImages().FindIcon_PackageAsync("PageEdit.png", Package.GetCurrentPackage(mod)),
                Location = ModuleAction.ActionLocationEnum.Any,
                Mode = ModuleAction.ActionModeEnum.Any,
                Style = ModuleAction.ActionStyleEnum.Nothing,
                LinkText = __ResStr("pageControlLink", "Control Panel"),
                MenuText = __ResStr("pageControlMenu", "Control Panel"),
                Tooltip = __ResStr("pageControlTT", "Control Panel - Add new or existing modules, add new pages, switch to edit mode, access page settings and other site management tasks"),
                Legend = __ResStr("pageControlLeg", "Control Panel - Adds new or existing modules, adds new pages, switches to edit mode, accesses page settings and other site management tasks"),
            };

            // <div id=IdPageControlDiv>
            //  action button (with id CssPageControlButton)
            //  module html...
            // </div>
            TagBuilder tag = new TagBuilder("div");
            tag.Attributes.Add("id", Globals.IdPageControlDiv);

            Manager.ForceModuleActionLinks = true; // so we get module action links (we're not in a pane)
            tag.SetInnerHtml((await action.RenderAsButtonIconAsync(Globals.IdPageControlButton)).ToString() + (await mod.RenderModuleAsync(htmlHelper)).ToString());
            Manager.ForceModuleActionLinks = false;
            return tag.ToHtmlString(TagRenderMode.Normal);
        }

#if MVC6
        public static async Task<HtmlString> RenderEditControlAsync<TYPE>(this IHtmlHelper<object> htmlHelper) {
#else
        public static async Task<HtmlString> RenderEditControlAsync<TYPE>(this HtmlHelper<object> htmlHelper) {
#endif
            Guid permGuid = ModuleDefinition.GetPermanentGuid(typeof(TYPE));
            return await htmlHelper.RenderEditControlAsync(permGuid);
        }

#if MVC6
        private static async Task<HtmlString> RenderEditControlAsync(this IHtmlHelper<object> htmlHelper, Guid moduleGuid) {
#else
        private static async Task<HtmlString> RenderEditControlAsync(this HtmlHelper<object> htmlHelper, Guid moduleGuid) {
#endif
            if (Manager.IsInPopup) return HtmlStringExtender.Empty;
            //if (Manager.CurrentPage == null || Manager.CurrentPage.Temporary) return HtmlStringExtender.Empty;

            if (Manager.CurrentPage.IsAuthorized_Edit()) {

                ModuleDefinition mod = await ModuleDefinition.LoadAsync(moduleGuid);

                // <div class=CssEditControlDiv>
                //  action button (with id CssEditControlButton)
                // </div>
                TagBuilder tag = new TagBuilder("div");
                tag.Attributes.Add("id", Globals.IdEditControlDiv);


                ModuleAction action = await mod.GetModuleActionAsync(Manager.EditMode ? "SwitchToView" : "SwitchToEdit");
                if (Manager.EditMode) {
                    action.LinkText = __ResStr("editControlLinkToEdit", "Switch to Edit Mode");
                    action.MenuText = __ResStr("editControlLinkToEdit", "Switch to Edit Mode");
                } else {
                    action.LinkText = __ResStr("editControlLinkToView", "Switch to View Mode");
                    action.MenuText = __ResStr("editControlLinkToView", "Switch to View Mode");
                }
                tag.SetInnerHtml((await action.RenderAsButtonIconAsync(Globals.IdEditControlButton)).ToString() + (await mod.RenderModuleAsync(htmlHelper)).ToString());// mainly just to get js/css, the module is normally empty

                return tag.ToHtmlString(TagRenderMode.Normal);
            } else
                return HtmlStringExtender.Empty;
        }

        // unique, possibly new, typically used in skin to create unique non-pane modules
#if MVC6
        public static async Task<HtmlString> RenderModuleAsync<TYPE>(this IHtmlHelper htmlHelper, Action<TYPE> initModule = null) {
#else
        public static async Task<HtmlString> RenderModuleAsync<TYPE>(this HtmlHelper<object> htmlHelper, Action<TYPE> initModule = null) {
#endif
            return await htmlHelper.RenderUniqueModuleAsync(typeof(TYPE), (mod) => {
                if (initModule != null)
                    initModule((TYPE)mod);
            });
        }
#if MVC6
        public static async Task<HtmlString> RenderUniqueModuleAsync(this IHtmlHelper htmlHelper, Type modType, Action<object> initModule = null) {
#else
        public static async Task<HtmlString> RenderUniqueModuleAsync(this HtmlHelper htmlHelper, Type modType, Action<object> initModule = null) {
#endif
            Guid permGuid = ModuleDefinition.GetPermanentGuid(modType);
            ModuleDefinition mod = null;
            try {
                mod = await ModuleDefinition.LoadModuleDefinitionAsync(permGuid);
                if (mod == null) {
                    // doesn't exist, lock and try again
                    using (ILockObject lockObject = await ModuleDefinition.LockModuleAsync(permGuid)) {
                        mod = await ModuleDefinition.LoadModuleDefinitionAsync(permGuid);
                        if (mod == null) {
                            mod = ModuleDefinition.CreateNewDesignedModule(permGuid, null, null);
                            if (!mod.IsModuleUnique)
                                throw new InternalError("{0} is not a unique module (must specify a module guid)", modType.FullName);
                            mod.ModuleGuid = permGuid;
                            mod.Temporary = false;
                            if (initModule != null)
                                initModule(mod);
                            await mod.SaveAsync();
                        } else {
                            if (!mod.IsModuleUnique)
                                throw new InternalError("{0} is not a unique module (must specify a module guid)", modType.FullName);
                            mod.Temporary = false;
                        }
                        await lockObject.UnlockAsync();
                    }
                }
            } catch (Exception exc) {
                HtmlBuilder hb = ModuleDefinition.ProcessModuleError(exc, permGuid.ToString());
                return hb.ToHtmlString();
            }
            return await mod.RenderModuleAsync(htmlHelper);
        }
#if MVC6
        public static async Task<HtmlString> RenderReferencedModule_AjaxAsync(this IHtmlHelper htmlHelper, Type modType, Action<object> initModule = null) {
#else
        public static async Task<HtmlString> RenderReferencedModule_AjaxAsync(this HtmlHelper htmlHelper, Type modType, Action<object> initModule = null) {
#endif
            Guid permGuid = ModuleDefinition.GetPermanentGuid(modType);
            ModuleDefinition mod = null;
            try {
                mod = await ModuleDefinition.LoadModuleDefinitionAsync(permGuid);
                if (mod == null) {
                    // doesn't exist, lock and try again
                    using (ILockObject lockObject = await ModuleDefinition.LockModuleAsync(permGuid)) {
                        mod = await ModuleDefinition.LoadModuleDefinitionAsync(permGuid);
                        if (mod == null) {
                            mod = ModuleDefinition.CreateNewDesignedModule(permGuid, null, null);
                            if (!mod.IsModuleUnique)
                                throw new InternalError("{0} is not a unique module (must specify a module guid)", modType.FullName);
                            mod.ModuleGuid = permGuid;
                            mod.Temporary = false;
                            if (initModule != null)
                                initModule(mod);
                            await mod.SaveAsync();
                        } else {
                            if (!mod.IsModuleUnique)
                                throw new InternalError("{0} is not a unique module (must specify a module guid)", modType.FullName);
                            mod.Temporary = false;
                        }
                        await lockObject.UnlockAsync();
                    }
                }
            } catch (Exception exc) {
                HtmlBuilder hb = ModuleDefinition.ProcessModuleError(exc, permGuid.ToString());
                return hb.ToHtmlString();
            }
            return await mod.RenderReferencedModule_AjaxAsync(htmlHelper);
        }

        // non-unique, possibly new, typically used in skin to create non-unique non-pane modules

#if MVC6
        public static async Task<HtmlString> RenderModuleAsync<TYPE>(this IHtmlHelper<object> htmlHelper, Guid moduleGuid, Action<TYPE> initModule = null)
#else
        public static async Task<HtmlString> RenderModuleAsync<TYPE>(this HtmlHelper<object> htmlHelper, Guid moduleGuid, Action<TYPE> initModule = null)
#endif
        {
            ModuleDefinition mod = null;
            try {
                mod = await ModuleDefinition.LoadModuleDefinitionAsync(moduleGuid);
                if (mod == null) {
                    // doesn't exist, lock and try again
                    using (ILockObject lockObject = await ModuleDefinition.LockModuleAsync(moduleGuid)) {
                        mod = await ModuleDefinition.LoadModuleDefinitionAsync(moduleGuid);
                        if (mod == null) {
                            Guid permGuid = ModuleDefinition.GetPermanentGuid(typeof(TYPE));
                            mod = ModuleDefinition.CreateNewDesignedModule(permGuid, null, null);
                            if (mod.IsModuleUnique)
                                throw new InternalError("{0} is a unique module (can't specify a module guid)", typeof(TYPE).FullName);
                            mod.ModuleGuid = moduleGuid;
                            if (initModule != null)
                                initModule((TYPE)(object)mod);
                            mod.Temporary = false;
                            await mod.SaveAsync();
                        } else {
                            if (mod.IsModuleUnique)
                                throw new InternalError("{0} is a unique module (can't specify a module guid)", typeof(TYPE).FullName);
                            mod.Temporary = false;
                        }
                        await lockObject.UnlockAsync();
                    }
                }
            } catch (Exception exc) {
                HtmlBuilder hb = ModuleDefinition.ProcessModuleError(exc, moduleGuid.ToString());
                return hb.ToHtmlString();
            }
            return await mod.RenderModuleAsync(htmlHelper);
        }

#if MVC6
        public static async Task<HtmlString> RenderUniqueModuleAddOnsAsync(this IHtmlHelper htmlHelper, List<Guid> ExcludedGuids = null) {
#else
        public static async Task<HtmlString> RenderUniqueModuleAddOnsAsync(this HtmlHelper htmlHelper, List<Guid> ExcludedGuids = null) {
#endif
            Manager.Verify_NotPostRequest();
            List<AddOnManager.Module> mods = Manager.AddOnManager.GetAddedUniqueInvokedCssModules();
            HtmlBuilder hb = new HtmlBuilder();
            Manager.RenderingUniqueModuleAddons = true;
            foreach (AddOnManager.Module mod in mods) {
                if (!Manager.IsInPopup || mod.AllowInPopup) {
                    if (ExcludedGuids == null || !ExcludedGuids.Contains(mod.ModuleGuid))
                        hb.Append(await htmlHelper.RenderUniqueModuleAsync(mod.ModuleType));
                }
            }
            Manager.RenderingUniqueModuleAddons = false;
            return hb.ToHtmlString();
        }
#if MVC6
        public static async Task<HtmlString> RenderReferencedModule_AjaxAsync(this IHtmlHelper htmlHelper) {
#else
        public static async Task<HtmlString> RenderReferencedModule_AjaxAsync(this HtmlHelper htmlHelper) {
#endif
            Manager.Verify_PostRequest();
            List<AddOnManager.Module> mods = Manager.AddOnManager.GetAddedUniqueInvokedCssModules();
            HtmlBuilder hb = new HtmlBuilder();
            Manager.RenderingUniqueModuleAddonsAjax = true;
            foreach (AddOnManager.Module mod in mods) {
                if (mod.AllowInAjax)
                    hb.Append(await htmlHelper.RenderReferencedModule_AjaxAsync(mod.ModuleType));
            }
            Manager.RenderingUniqueModuleAddonsAjax = false;
            return hb.ToHtmlString();
        }
        public static void AddVolatileOptionsUniqueModuleAddOns(bool MarkPrevious = false) {
            Manager.Verify_NotPostRequest();
            List<AddOnManager.Module> mods = Manager.AddOnManager.GetAddedUniqueInvokedCssModules();
            Manager.ScriptManager.AddVolatileOption("Basics", MarkPrevious ? "UnifiedAddonModsPrevious" : "UnifiedAddonMods",
                (from m in mods where !Manager.IsInPopup || m.AllowInPopup select m.ModuleGuid).ToList());
        }
    }
}
