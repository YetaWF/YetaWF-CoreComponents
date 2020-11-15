/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

#nullable enable

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
    /// <remarks>Each package implements an area registration class deriving from AreaRegistrationBase.</remarks>
    public abstract class AreaRegistrationBase {
        /// <summary>
        /// Constructor.
        /// </summary>
        public AreaRegistrationBase() {
            Package = Package.GetPackageFromAssembly(GetType().Assembly);
        }
        /// <summary>
        /// The area name registered by the current package.
        /// </summary>
        /// <remarks>Packages define their area name using the PackageAttribute (for the domain portion) and the <see cref="System.Reflection.AssemblyProductAttribute"/> (for the product name). The area name is the concatenation of the domain, followed by an underscore and the product (e.g., YetaWF_Text).</remarks>
        public string AreaName { get { return Package.AreaName; } }

        /// <summary>
        /// The current package defined by the object derived from AreaRegistrationBase.
        /// </summary>
        protected Package Package { get; set; }

        /// <summary>
        /// Retrieves the current package defined by the object derived from AreaRegistrationBase.
        /// </summary>
        /// <returns>The Package object.</returns>
        protected Package GetCurrentPackage() { return Package; }

        /// <summary>
        /// Used internally to register area routes. Don't mess with this.
        /// </summary>
        public void RegisterArea(IEndpointRouteBuilder endpoints) {
            Logging.AddLog("Found {0} in namespace {1}", AreaName, GetType().Namespace!);
            endpoints.MapAreaControllerRoute(AreaName, AreaName, AreaName + "/{controller}/{action}/{*whatevz}");
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

