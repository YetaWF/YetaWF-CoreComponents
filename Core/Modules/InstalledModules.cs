/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YetaWF.Core.Log;
using YetaWF.Core.Models;
using YetaWF.Core.Packages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Modules {

    /// <summary>
    /// Describes all installed modules.
    /// </summary>
    public class InstalledModules : IInitializeApplicationStartup {

        // PROPERTIES, METHODS
        // PROPERTIES, METHODS
        // PROPERTIES, METHODS

        /// <summary>
        /// Describes an installed module.
        /// </summary>
        public class ModuleTypeEntry {
            /// <summary>
            /// The module type.
            /// </summary>
            public Type Type { get; set; } = null!;
            /// <summary>
            /// The package implementing this module.
            /// </summary>
            public Package Package { get; set; } = null!;
            /// <summary>
            /// The user displayable name of the module.
            /// </summary>
            public MultiString DisplayName { get; set; } = null!;
            /// <summary>
            /// The user displayable module description.
            /// </summary>
            public MultiString Summary { get; set; } = null!;
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable", Justification="Not used for serialization")]
        public class ModuleTypesDictionary : Dictionary<Guid, ModuleTypeEntry> { }

        /// <summary>
        /// Lists all packages that implement modules.
        /// </summary>
        public static List<Package> Packages { get; private set; } = null!;
        /// <summary>
        /// Lists all available modules.
        /// </summary>
        public static ModuleTypesDictionary Modules { get; private set; } = null!;

        /// <summary>
        /// Constructor.
        /// </summary>
        public InstalledModules() { }

        /// <summary>
        /// Find an installed module based on its permanent module Guid.
        /// </summary>
        /// <param name="permanentGuid">The module's permanent Guid.</param>
        /// <returns>The type of the module or null if not found.</returns>
        public static Type? TryFindModule(Guid permanentGuid) {
            ModuleTypeEntry? entry = TryFindModuleEntry(permanentGuid);
            if (entry == null)
                return null;
            return entry.Type;
        }
        /// <summary>
        /// Find an installed module entry based on its permanent module Guid.
        /// </summary>
        /// <param name="permanentGuid">The module's permanent Guid.</param>
        /// <returns>The module entry of the module or null if not found.</returns>
        public static ModuleTypeEntry? TryFindModuleEntry(Guid permanentGuid) {
            if (!Modules.TryGetValue(permanentGuid, out ModuleTypeEntry? entry))
                return null;
            return entry;
        }

        // STARTUP
        // STARTUP
        // STARTUP

        /// <summary>
        /// Initialization executed during application startup.
        /// </summary>
        public Task InitializeApplicationStartupAsync() {
            Packages = new List<Package>();
            Modules = new ModuleTypesDictionary();
            AddInstalledModules();
            return Task.CompletedTask;
        }

        // LOCATE ALL INSTALLED MODULES
        // LOCATE ALL INSTALLED MODULES
        // LOCATE ALL INSTALLED MODULES

        private static void AddInstalledModules() {

            Logging.AddLog("Building installed modules dictionary");

            foreach (Package package in Package.GetAvailablePackages()) {
                List<Type> modTypes = package.GetClassesInPackage<ModuleDefinition>();
                if (modTypes.Count() > 0) {
                    Packages.Add(package);
                    foreach (Type type in modTypes) {
                        try {
                            Guid guid = ModuleDefinition.GetPermanentGuid(type);
                            Logging.AddLog("Found module {0} ({1})", guid.ToString(), type.Namespace!);
                            if (type == typeof(ModuleDefinition))
                                continue;
                            object? obj = Activator.CreateInstance(type) ;
                            if (obj == null)
                                throw new InternalError("Module type {0} can't be created in AddInstalledModules", type.Name);
                            ModuleDefinition? mod = obj as ModuleDefinition;
                            if (mod == null)
                                throw new InternalError("Type {0} is not a module in AddInstalledModules", type.Name);
                            if (guid == Guid.Empty)
                                throw new InternalError("Invalid guid (empty);");
                            Modules.Add(guid, new ModuleTypeEntry {
                                Type = type,
                                Package = package,
                                DisplayName = mod.ModuleDisplayName,
                                Summary = mod.Description
                            });
                            // collect unique modules that are invoked by css on templates
                            if (mod.Invokable) {
                                if (!mod.IsModuleUnique)
                                    throw new InternalError("Only unique modules can be marked invokable ({0})", type.FullName);
                                YetaWFManager.Manager.AddOnManager.AddUniqueInvokedCssModule(type, mod.ModuleGuid, mod.SupportedTemplates, mod.InvokingCss, mod.InvokeInPopup, mod.InvokeInAjax);
                            }
                        } catch (Exception exc) {
                            Logging.AddErrorLog("Module class {0} failed.", exc);
                        }
                    }
                }
            }

            Logging.AddLog("Installed modules dictionary completed");
        }
    }
}
