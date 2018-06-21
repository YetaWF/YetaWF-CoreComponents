/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
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

    /// <summary>
    /// Base class for area registration.
    /// </summary>
    /// <remarks>Each package implements an area registration class deriving from AreaRegistrationBase.</remarks>
#if MVC6
    public abstract class AreaRegistrationBase {
#else
    public abstract class AreaRegistrationBase : System.Web.Mvc.AreaRegistration {
#endif
        /// <summary>
        /// Constructor.
        /// </summary>
        public AreaRegistrationBase() {
            Package = Package.GetPackageFromAssembly(GetType().Assembly);
        }
        /// <summary>
        /// The area name registered by the current package.
        /// </summary>
        /// <remarks>Packages define their area name using the PackageAttribute (for the domain portion) and the AssemblyProduct (for the product name). The area name is the concatenation of the domain, followed by an underscore and the product (e.g., YetaWF_Text).</remarks>
#if MVC6
        public string AreaName { get { return Package.AreaName; } }
#else
        public override string AreaName { get { return Package.AreaName; } }
#endif
        protected Package Package { get; set; }

        /// <summary>
        /// Retrieves the current package defined by the object derived from AreaRegistrationBase.
        /// </summary>
        /// <returns>The Package object.</returns>
        protected Package GetCurrentPackage() { return Package; }

        /// <summary>
        /// Used internally to register area routes. Don't mess with this.
        /// </summary>
#if MVC6
        public void RegisterArea(IRouteBuilder routes) {
            Logging.AddLog("Found {0} in namespace {1}", AreaName, GetType().Namespace);
            routes.MapAreaRoute(
                AreaName,
                AreaName,
                AreaName + "/{controller}/{action}/{*whatevz}"
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
                new string[] { ns }
            );
        }
#endif

        /// <summary>
        /// Used by tools (i.e., non web apps) that need to explicitly register packages to they have access to functionality provided by packages, beyond the Core package.
        /// </summary>
        /// <remarks>This is typically used by tools that need access to data providers used by YetaWF.</remarks>
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
            return Package.GetClassesInPackages<AreaRegistrationBase>();
        }
    }
}

