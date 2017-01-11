/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using YetaWF.Core.Addons;
using YetaWF.Core.Log;
using YetaWF.Core.Packages;

namespace YetaWF.Core.Support {

    public interface IInitializeApplicationStartup { // any class defining this interface is called during application startup
        void InitializeApplicationStartup();
    }

    public static class Startup {

        public static bool Started { get; set; }

        public static void CallStartupClasses() {
            Logging.AddLog("Processing IInitializeApplicationStartup");
            // get all types, but put the VersionManagerStartup class first
            List<Type> types = GetStartupTypes();
            Type versionManager = typeof(VersionManagerStartup);
            types.Remove(versionManager);
            types.Insert(0, versionManager);
            // now start up all classes
            foreach (Type type in types) {
                try {
                    IInitializeApplicationStartup iStart = (IInitializeApplicationStartup) Activator.CreateInstance(type);
                    if (iStart != null) {
                        Logging.AddLog("Calling global startup class \'{0}\'", type.FullName);
                        iStart.InitializeApplicationStartup();
                    }
                } catch (Exception exc) {
                    Logging.AddErrorLog("Startup class {0} failed.", type.FullName, exc);
                    throw;
                }
            }
            Logging.AddLog("Processing IInitializeApplicationStartup Ended");
        }
        private static List<Type> GetStartupTypes() {
            List<Type> moduleTypes = new List<Type>();

            List<Package> packages = Package.GetAvailablePackages();
            // order all available packages by service level so we start them up in the correct order
            packages = (from p in packages orderby (int)p.ServiceLevel select p).ToList();

            foreach (Package package in packages) {

                Type[] typesInAsm;
                try {
                    typesInAsm = package.PackageAssembly.GetTypes();
                } catch (ReflectionTypeLoadException ex) {
                    typesInAsm = ex.Types;
                }
                Type[] modTypes = typesInAsm.Where(type => IsStartupType(type)).ToArray<Type>();
                moduleTypes.AddRange(modTypes);
            }
            return moduleTypes;
        }
        private static bool IsStartupType(Type type) {
            if (!TypeIsPublicClass(type))
                return false;
            return typeof(IInitializeApplicationStartup).IsAssignableFrom(type);
        }
        private static bool TypeIsPublicClass(Type type) {
            return (type != null && type.IsPublic && type.IsClass && !type.IsAbstract && !type.IsGenericType);
        }
    }
}
