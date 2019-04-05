/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

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
using YetaWF.Core.Pages;
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

        public static async Task<string> RenderPageControlAsync<TYPE>(this YHtmlHelper htmlHelper) {
            Guid permGuid = ModuleDefinition.GetPermanentGuid(typeof(TYPE));
            return await htmlHelper.RenderPageControlAsync(permGuid);
        }

        public static async Task<string> RenderPageControlAsync(this YHtmlHelper htmlHelper, Guid moduleGuid) {
            if (Manager.IsInPopup) return null;
            if (Manager.CurrentPage == null || Manager.CurrentPage.Temporary) return null;

#if DEBUG
            // allow in debug mode without checking unless marked deployed
            if (Manager.Deployed && !Manager.CurrentPage.IsAuthorized_Edit()) return null;
#else
            if (!Manager.CurrentPage.IsAuthorized_Edit()) return null;
#endif
            ModuleDefinition mod = await ModuleDefinition.LoadAsync(moduleGuid);

            string css = null;
            if (Manager.SkinInfo.UsingBootstrap && Manager.SkinInfo.UsingBootstrapButtons)
                css = "btn btn-outline-primary";
            ModuleAction action = new ModuleAction(mod) {
                Category = ModuleAction.ActionCategoryEnum.Significant,
                CssClass = css,
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
            tag.SetInnerHtml(await action.RenderAsButtonIconAsync(Globals.IdPageControlButton) + await mod.RenderModuleAsync(htmlHelper));
            Manager.ForceModuleActionLinks = false;
            return tag.ToString(TagRenderMode.Normal);
        }

        public static async Task<string> RenderEditControlAsync<TYPE>(this YHtmlHelper htmlHelper) {
            Guid permGuid = ModuleDefinition.GetPermanentGuid(typeof(TYPE));
            return await htmlHelper.RenderEditControlAsync(permGuid);
        }

        public static async Task<string> RenderEditControlAsync(this YHtmlHelper htmlHelper, Guid moduleGuid) {

            if (Manager.IsInPopup) return null;
            //if (Manager.CurrentPage == null || Manager.CurrentPage.Temporary) return null;

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
                if (Manager.SkinInfo.UsingBootstrap && Manager.SkinInfo.UsingBootstrapButtons)
                    action.CssClass = CssManager.CombineCss(action.CssClass, "btn btn-outline-primary");
                tag.SetInnerHtml(await action.RenderAsButtonIconAsync(Globals.IdEditControlButton) + await mod.RenderModuleAsync(htmlHelper));// mainly just to get js/css, the module is normally empty

                return tag.ToString(TagRenderMode.Normal);
            } else
                return null;
        }

        /// <summary>
        /// Renders a unique module. This is typically used in pages to use or create unique modules, which are not part of a pane.
        /// </summary>
        /// <typeparam name="TYPE">The type of the module.</typeparam>
        /// <param name="htmlHelper">The HtmlHelper instance.</param>
        /// <param name="initModule">An optional callback to initialize the module if it is a new module. This is called after the module has been created. The action parameter is the module instance.</param>
        /// <returns>The module rendered as HTML.</returns>
        public static async Task<string> RenderUniqueModuleAsync<TYPE>(this YHtmlHelper htmlHelper, Action<TYPE> initModule = null) {
            return await htmlHelper.RenderUniqueModuleAsync(typeof(TYPE), (mod) => {
                if (initModule != null)
                    initModule((TYPE)(object)mod);
            });
        }

        /// <summary>
        /// Renders a unique module. This is typically used in pages to use or create unique modules, which are not part of a pane.
        /// </summary>
        /// <param name="packageName">The name of the package implementing the module.</param>
        /// <param name="typeName">The fully qualified type name of the module.</param>
        /// <param name="htmlHelper">The HtmlHelper instance.</param>
        /// <param name="initModule">An optional callback to initialize the module if it is a new module. This is called after the module has been created. The action parameter is the module instance.</param>
        /// <returns>The module rendered as HTML.</returns>
        public static async Task<string> RenderUniqueModuleAsync(this YHtmlHelper htmlHelper, string packageName, string typeName, Action<ModuleDefinition> initModule = null) {
            Package package = Package.GetPackageFromPackageName(packageName);
            Type type = package.PackageAssembly.GetType(typeName, true);
            return await htmlHelper.RenderUniqueModuleAsync(type, (mod) => {
                if (initModule != null)
                    initModule(mod);
            });
        }
        internal static async Task<string> RenderUniqueModuleAsync(this YHtmlHelper htmlHelper, Type modType, Action<ModuleDefinition> initModule = null) {
            Guid permGuid = ModuleDefinition.GetPermanentGuid(modType);
            ModuleDefinition mod = null;
            try {
                mod = await Module.LoadModuleDefinitionAsync(permGuid);
                if (mod == null) {
                    // doesn't exist, lock and try again
                    using (ILockObject lockObject = await Module.LockModuleAsync(permGuid)) {
                        mod = await Module.LoadModuleDefinitionAsync(permGuid);
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
                mod.Temporary = false;
            } catch (Exception exc) {
                HtmlBuilder hb = ModuleDefinition.ProcessModuleError(exc, permGuid.ToString());
                return hb.ToString();
            }
            return await mod.RenderModuleAsync(htmlHelper);
        }

        /// <summary>
        /// Renders a non-unique module. This is typically used in pages to use or create modules, which are not part of a pane.
        /// </summary>
        /// <typeparam name="TYPE">The type of the module.</typeparam>
        /// <param name="moduleGuid">The module Guid of the module to render. If the module doesn't exist, it is created.</param>
        /// <param name="htmlHelper">The HtmlHelper instance.</param>
        /// <param name="initModule">An optional callback to initialize the module if it is a new module. This is called after the module has been created. The action parameter is the module instance.</param>
        /// <returns>The module rendered as HTML.</returns>
        public static async Task<string> RenderModuleAsync<TYPE>(this YHtmlHelper htmlHelper, Guid moduleGuid, Action<TYPE> initModule = null) {
            ModuleDefinition mod = null;
            try {
                mod = await Module.LoadModuleDefinitionAsync(moduleGuid);
                if (mod == null) {
                    // doesn't exist, lock and try again
                    using (ILockObject lockObject = await Module.LockModuleAsync(moduleGuid)) {
                        mod = await Module.LoadModuleDefinitionAsync(moduleGuid);
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
                mod.Temporary = false;
            } catch (Exception exc) {
                HtmlBuilder hb = ModuleDefinition.ProcessModuleError(exc, moduleGuid.ToString());
                return hb.ToString();
            }
            return await mod.RenderModuleAsync(htmlHelper);
        }

        public static async Task<string> RenderReferencedModule_AjaxAsync(this YHtmlHelper htmlHelper, Type modType, Action<object> initModule = null) {
            Guid permGuid = ModuleDefinition.GetPermanentGuid(modType);
            ModuleDefinition mod = null;
            try {
                mod = await Module.LoadModuleDefinitionAsync(permGuid);
                if (mod == null) {
                    // doesn't exist, lock and try again
                    using (ILockObject lockObject = await Module.LockModuleAsync(permGuid)) {
                        mod = await Module.LoadModuleDefinitionAsync(permGuid);
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
                mod.Temporary = false;
            } catch (Exception exc) {
                HtmlBuilder hb = ModuleDefinition.ProcessModuleError(exc, permGuid.ToString());
                return hb.ToString();
            }
            return await mod.RenderReferencedModule_AjaxAsync(htmlHelper);
        }

        public static async Task<string> RenderUniqueModuleAddOnsAsync(this YHtmlHelper htmlHelper, List<Guid> ExcludedGuids = null) {

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
            return hb.ToString();
        }

        public static async Task<string> RenderReferencedModule_AjaxAsync(this YHtmlHelper htmlHelper) {
            Manager.Verify_PostRequest();
            List<AddOnManager.Module> mods = Manager.AddOnManager.GetAddedUniqueInvokedCssModules();
            HtmlBuilder hb = new HtmlBuilder();
            Manager.RenderingUniqueModuleAddonsAjax = true;
            foreach (AddOnManager.Module mod in mods) {
                if (mod.AllowInAjax)
                    hb.Append(await htmlHelper.RenderReferencedModule_AjaxAsync(mod.ModuleType));
            }
            Manager.RenderingUniqueModuleAddonsAjax = false;
            return hb.ToString();
        }
        public static void AddVolatileOptionsUniqueModuleAddOns(bool MarkPrevious = false) {
            Manager.Verify_NotPostRequest();
            List<AddOnManager.Module> mods = Manager.AddOnManager.GetAddedUniqueInvokedCssModules();
            Manager.ScriptManager.AddVolatileOption("Basics", MarkPrevious ? "UnifiedAddonModsPrevious" : "UnifiedAddonMods",
                (from m in mods where !Manager.IsInPopup || m.AllowInPopup select m.ModuleGuid).ToList());
        }
    }
}
