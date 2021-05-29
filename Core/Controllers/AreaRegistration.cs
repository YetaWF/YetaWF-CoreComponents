/* Copyright © 2021 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using YetaWF.Core.Log;
using YetaWF.Core.Packages;

namespace YetaWF.Core.Controllers {

    /// <summary>
    /// Base class for area registration.
    /// </summary>
    /// <remarks>
    /// An instance of this class is instantiated and initialized during application startup in order to define the MVC area used by a package.
    /// Each package defines its own MVC area. The name is derived from the YetaWF.PackageAttributes.PackageAttribute (for the domain portion) 
    /// and the <see cref="System.Reflection.AssemblyProductAttribute"/> (for the product name),
    /// defined in the package's AssemblyInfo.cs source file.
    ///
    /// The area name is the concatenation of the domain, followed by an underscore and the product name (e.g., YetaWF_Text).
    ///
    /// Applications can reference the current package using the static CurrentPackage property.
    ///
    /// Applications do not instantiate this class.
    /// </remarks>    
    public abstract class AreaRegistrationBase {
        /// <summary>
        /// Constructor.
        /// </summary>
        protected AreaRegistrationBase() { }

        /// <summary>
        /// Used internally to register area routes. Don't mess with this.
        /// </summary>
        public void RegisterArea(IEndpointRouteBuilder endpoints) {
            Package package = Package.GetPackageFromAssembly(GetType().Assembly);
            string areaName = package.AreaName;
            Logging.AddLog("Found {0} in namespace {1}", areaName, GetType().Namespace!);
            endpoints.MapAreaControllerRoute(areaName, areaName, areaName + "/{controller}/{action}/{*whatevz}");
        }

        /// <summary>
        /// Used by tools (i.e., non web apps) that need to explicitly register packages so they have access to functionality provided by packages, beyond the Core package.
        /// </summary>
        /// <remarks>This is typically used by tools that need access to data providers used by YetaWF.</remarks>
        public static void RegisterPackages(IEndpointRouteBuilder? endpoints = null) {
            Logging.AddLog("Processing RegisterPackages");
            List<Type> types = GetAreaRegistrationTypes();
            foreach (Type type in types) {
                try {
                    dynamic? areaReg = Activator.CreateInstance(type);
                    if (areaReg != null) {
                        Logging.AddLog("AreaRegistration class \'{0}\' found", type.FullName!);
                        if (endpoints != null)
                            areaReg.RegisterArea(endpoints);
                    }
                } catch (Exception exc) {
                    Logging.AddErrorLog("AreaRegistration class {0} failed.", type.FullName!, exc);
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

