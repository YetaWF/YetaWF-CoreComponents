/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;
using YetaWF.Core.Log;
using YetaWF.Core.Packages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Controllers {

    // RESEARCH: http://stephenwalther.com/archive/2015/02/07/asp-net-5-deep-dive-routing

    public abstract class AreaRegistrationBase : System.Web.Mvc.AreaRegistration {

        public Package Package { get; set; }
        public override string AreaName { get { return Package.AreaName; } }

        public AreaRegistrationBase() {
            Package = Package.GetPackageFromAssembly(GetType().Assembly);
        }
        protected Package GetCurrentPackage() {
            return Package;
        }

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
        public static void RegisterPackages() {
            Logging.AddLog("Processing RegisterPackages");
            if (YetaWFManager.Manager.HostUsed != YetaWFManager.BATCHMODE)
                throw new InternalError("RegisterPackages can only be used in batch mode");
            List<Type> types = GetAreaRegistrationTypes();
            foreach (Type type in types) {
                try {
                    dynamic areaReg = Activator.CreateInstance(type);
                    if (areaReg != null) {
                        Logging.AddLog("AreaRegistration class \'{0}\' found", type.FullName);
                    }
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
