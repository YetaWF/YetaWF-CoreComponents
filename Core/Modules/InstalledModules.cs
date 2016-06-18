/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using YetaWF.Core.Log;
using YetaWF.Core.Models;
using YetaWF.Core.Packages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Modules {

    public class InstalledModules : IInitializeApplicationStartup {

        // PROPERTIES, METHODS
        // PROPERTIES, METHODS
        // PROPERTIES, METHODS

        public class ModuleTypeEntry {
            public Type Type { get; set; }
            public MultiString DisplayName { get; set; }
            public MultiString Summary { get; set; }
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable", Justification="Not used for serialization")]
        public class ModuleTypesDictionary : Dictionary<Guid, ModuleTypeEntry> { }

        public static List<Assembly> Assemblies { get; private set; }
        public static ModuleTypesDictionary Modules { get; private set; }

        public InstalledModules() { }

        public static Type TryFindModule(Guid permanentGuid) {
            ModuleTypeEntry entry = TryFindModuleEntry(permanentGuid);
            if (entry == null)
                return null;
            return entry.Type;
        }
        public static ModuleTypeEntry TryFindModuleEntry(Guid permanentGuid) {
            ModuleTypeEntry entry;
            if (!Modules.TryGetValue(permanentGuid, out entry))
                return null;
            return entry;
        }

        // STARTUP
        // STARTUP
        // STARTUP

        public void InitializeApplicationStartup() {
            Assemblies = new List<Assembly>();
            Modules = new ModuleTypesDictionary();
            AddInstalledModules();
        }

        // LOCATE ALL INSTALLED MODULES
        // LOCATE ALL INSTALLED MODULES
        // LOCATE ALL INSTALLED MODULES

        private static void AddInstalledModules() {

            Logging.AddLog("Building installed modules dictionary");
            List<Type> types = GetModuleTypes();
            foreach (Type type in types) {
                try {
                    Guid guid = ModuleDefinition.GetPermanentGuid(type);
                    Logging.AddLog("Found module {0} ({1})",  guid.ToString(), type.Namespace);
                    if (type == typeof(ModuleDefinition))
                        continue;
                    object obj = Activator.CreateInstance(type);
                    if (obj == null)
                        throw new InternalError("Module type {0} can't be created in AddInstalledModules", type.Name);
                    ModuleDefinition mod = obj as ModuleDefinition;
                    if (mod == null)
                        throw new InternalError("Type {0} is not a module in AddInstalledModules", type.Name);
                    if (guid == Guid.Empty)
                        throw new InternalError("Invalid guid (empty);");
                    Modules.Add(guid, new ModuleTypeEntry { 
                        Type = type,
                        DisplayName = mod.ModuleDisplayName,
                        Summary = mod.Description
                    });
                    // collect unique modules that are invoked by css on templates
                    if (mod.Invokable) {
                        if (!mod.IsModuleUnique)
                            throw new InternalError("Only unique modules can be invokable");
                        YetaWFManager.Manager.AddOnManager.AddUniqueInvokedCssModule(type, mod.ModuleGuid, mod.SupportedTemplates, mod.InvokingCss, mod.InvokeInPopup, mod.InvokeInAjax);
                    }
                } catch (Exception exc) {
                    Logging.AddErrorLog("Module class {0} failed.", exc);
                }
            }
            Logging.AddLog("Installed modules dictionary completed");
        }

        private static List<Type> GetModuleTypes() {
            List<Type> moduleTypes = new List<Type>();

            foreach (Package package in Package.GetAvailablePackages()) {
                Assembly assembly = package.PackageAssembly;
                Type[] typesInAsm;
                try {
                    typesInAsm = assembly.GetTypes();
                } catch (ReflectionTypeLoadException ex) {
                    typesInAsm = ex.Types;
                }
                Type[] modTypes = typesInAsm.Where(type => IsModuleType(type)).ToArray<Type>();
                if (modTypes.Count() > 0)
                    Assemblies.Add(assembly);
                moduleTypes.AddRange(modTypes);
            }
            return moduleTypes;
        }
        private static bool IsModuleType(Type type) {
            if (!TypeIsPublicClass(type))
                return false;
            return typeof(ModuleDefinition).IsAssignableFrom(type); // this includes the ModuleDefinition class itself so we get the Core
        }
        private static bool TypeIsPublicClass(Type type) {
            return (type != null && type.IsPublic && type.IsClass && !type.IsAbstract);
        }
    }
}
