/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YetaWF.Core.Addons;
using YetaWF.Core.IO;
using YetaWF.Core.Localize;
using YetaWF.Core.Packages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Modules {

    public static class ModuleDefinitionExtensions {

        private static string __ResStr(string name, string defaultValue, params object?[] parms) { return ResourceAccess.GetResourceString(typeof(ModuleDefinitionExtensions), name, defaultValue, parms); }

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        /// <summary>
        /// Renders a unique module. This is typically used in pages to use or create unique modules, which are not part of a pane.
        /// </summary>
        /// <typeparam name="TYPE">The type of the module.</typeparam>
        /// <param name="htmlHelper">The HtmlHelper instance.</param>
        /// <param name="initModule">An optional callback to initialize the module if it is a new module. This is called after the module has been created. The action parameter is the module instance.</param>
        /// <returns>The module rendered as HTML.</returns>
        public static async Task<string> RenderUniqueModuleAsync<TYPE>(this YHtmlHelper htmlHelper, Action<TYPE>? initModule = null) {
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
        public static async Task<string> RenderUniqueModuleAsync(this YHtmlHelper htmlHelper, string packageName, string typeName, Action<ModuleDefinition>? initModule = null) {
            Package package = Package.GetPackageFromPackageName(packageName);
            Type type = package.PackageAssembly.GetType(typeName, true) !;
            return await htmlHelper.RenderUniqueModuleAsync(type, (mod) => {
                if (initModule != null)
                    initModule(mod);
            });
        }
        internal static async Task<string> RenderUniqueModuleAsync(this YHtmlHelper htmlHelper, Type modType, Action<ModuleDefinition>? initModule = null) {
            Guid permGuid = ModuleDefinition.GetPermanentGuid(modType);
            ModuleDefinition? mod = null;
            try {
                mod = await Module.LoadModuleDefinitionAsync(permGuid);
                if (mod == null) {
                    // doesn't exist, lock and try again
                    await using (ILockObject lockObject = await Module.LockModuleAsync(permGuid)) {
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
        public static async Task<string> RenderModuleAsync<TYPE>(this YHtmlHelper htmlHelper, Guid moduleGuid, Action<TYPE>? initModule = null) {
            ModuleDefinition? mod = null;
            try {
                mod = await Module.LoadModuleDefinitionAsync(moduleGuid);
                if (mod == null) {
                    // doesn't exist, lock and try again
                    await using (ILockObject lockObject = await Module.LockModuleAsync(moduleGuid)) {
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
                    }
                }
                mod.Temporary = false;
            } catch (Exception exc) {
                HtmlBuilder hb = ModuleDefinition.ProcessModuleError(exc, moduleGuid.ToString());
                return hb.ToString();
            }
            return await mod.RenderModuleAsync(htmlHelper);
        }

        public static async Task<string> RenderReferencedModule_AjaxAsync(this YHtmlHelper htmlHelper, Type modType, Action<object>? initModule = null) {
            Guid permGuid = ModuleDefinition.GetPermanentGuid(modType);
            ModuleDefinition? mod = null;
            try {
                mod = await Module.LoadModuleDefinitionAsync(permGuid);
                if (mod == null) {
                    // doesn't exist, lock and try again
                    await using (ILockObject lockObject = await Module.LockModuleAsync(permGuid)) {
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
                    }
                }
                mod.Temporary = false;
            } catch (Exception exc) {
                HtmlBuilder hb = ModuleDefinition.ProcessModuleError(exc, permGuid.ToString());
                return hb.ToString();
            }
            return await mod.RenderReferencedModule_AjaxAsync(htmlHelper);
        }

        public static async Task<string> RenderUniqueModuleAddOnsAsync(this YHtmlHelper htmlHelper, List<Guid>? ExcludedGuids = null) {

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

        public static Task<string> RenderPageStatus(this YHtmlHelper htmlHelper, bool WantNoJavaScript = true, bool WantLocked = true) {
            HtmlBuilder hb = new HtmlBuilder();
            if (WantNoJavaScript) {
                hb.Append($@"<noscript><div class='{Globals.CssDivWarning}' style='height:100px;text-align:center;vertical-align:middle'>{Utility.HE(__ResStr("reqJS", "This site requires Javascript"))}</div></noscript>");
            }
            if (WantLocked && Manager.CurrentSite.IsLockedAny) {
                hb.Append($@"<div class='{Globals.CssDivAlert}'>{Utility.HE(__ResStr("locked", "Site is locked!"))}</div>");
            }
            return Task.FromResult(hb.ToString());
        }
    }
}
