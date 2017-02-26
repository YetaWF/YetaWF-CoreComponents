/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using YetaWF.Core.Log;
using YetaWF.Core.Packages;
using YetaWF.Core.Support;
#if MVC6
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
#else
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Controllers {

#if MVC6
    public abstract class AreaRegistrationBase {
#else
    public abstract class AreaRegistrationBase : System.Web.Mvc.AreaRegistration {
#endif
        public AreaRegistrationBase() {
            Package = Package.GetPackageFromAssembly(GetType().Assembly);
        }
#if MVC6
        public string AreaName { get { return Package.AreaName; } }
#else
        public override string AreaName { get { return Package.AreaName; } }
#endif
        public Package Package { get; set; }
        protected Package GetCurrentPackage() { return Package; }

#if MVC6
        public void RegisterArea(IRouteBuilder routes) {
            Logging.AddLog("Found {0} in namespace {1}", AreaName, GetType().Namespace);
            routes.MapAreaRoute(
                AreaName,
                AreaName,
                AreaName +"/{controller}/{action}"
            );
        }
#else
        public override void RegisterArea(AreaRegistrationContext context) {
            Logging.AddLog("Found {0} in namespace {1}", AreaName, GetType().Namespace);

            string ns = GetType().Namespace;

            context.MapRoute(
                AreaName,
                AreaName + "/{controller}/{action}",
                new { },
                new string[] { ns, ns + ".Shared" }
            );
        }
#endif

#if MVC6
        public static void RegisterPackages(IRouteBuilder routes = null) {
#else
        public static void RegisterPackages() {
#endif
            Logging.AddLog("Processing RegisterPackages");
#if MVC6
#else
            if (YetaWFManager.Manager.HostUsed != YetaWFManager.BATCHMODE)
                throw new InternalError("RegisterPackages can only be used in batch mode");
#endif
            List<Type> types = GetAreaRegistrationTypes();
            foreach (Type type in types) {
                try {
                    dynamic areaReg = Activator.CreateInstance(type);
                    if (areaReg != null) {
                        Logging.AddLog("AreaRegistration class \'{0}\' found", type.FullName);
                    }
#if MVC6
                    if (routes != null)
                        areaReg.RegisterArea(routes);
#else
#endif
                } catch (Exception exc) {
                    Logging.AddErrorLog("AreaRegistration class {0} failed.", type.FullName, exc);
                    throw;
                }
            }
            Logging.AddLog("Processing RegisterPackages Ended");
        }
        private static List<Type> GetAreaRegistrationTypes() {
            List<Type> regTypes = new List<Type>();
            List<Package> packages = Package.GetAvailablePackages();
            foreach (Package package in packages) {
                Type[] typesInAsm;
                try {
                    typesInAsm = package.PackageAssembly.GetTypes();
                } catch (ReflectionTypeLoadException ex) {
                    typesInAsm = ex.Types;
                }
                Type[] modTypes = typesInAsm.Where(type => IsAreaRegistrationType(type)).ToArray<Type>();
                regTypes.AddRange(modTypes);
            }
            return regTypes;
        }
        private static bool IsAreaRegistrationType(Type type) {
            if (!TypeIsPublicClass(type))
                return false;
            return typeof(YetaWF.Core.Controllers.AreaRegistrationBase).IsAssignableFrom(type);
        }
        private static bool TypeIsPublicClass(Type type) {
            return (type != null && type.IsPublic && type.IsClass && !type.IsAbstract && !type.IsGenericType);
        }
    }
}

