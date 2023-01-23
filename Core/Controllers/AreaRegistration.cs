/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Reflection;
using YetaWF.Core.Endpoints;
using YetaWF.Core.Log;
using YetaWF.Core.Packages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Controllers {

    /// <summary>
    /// Used for area registration.
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
        /// Used to register all package areas and endpoints.
        /// </summary>
        public static void RegisterPackages(IEndpointRouteBuilder? endpoints = null) {
            Logging.AddLog($"Processing {nameof(RegisterPackages)}");
            // Areas
            List<Type> types = GetAreaRegistrationTypes();
            foreach (Type type in types) {
                try {
                    dynamic? areaReg = Activator.CreateInstance(type);
                    if (areaReg != null) {
                        Logging.AddLog($"{nameof(RegisterPackages)} class \'{0}\' found", type.FullName!);
                        if (endpoints != null)
                            areaReg.RegisterArea(endpoints);
                    }
                } catch (Exception exc) {
                    Logging.AddErrorLog($"{nameof(RegisterPackages)} class {0} failed.", type.FullName!, exc);
                    throw;
                }
            }
            // Endpoints
            if (endpoints != null) {
                types = GetEndpointRegistrationTypes();
                foreach (Type type in types) {
                    if (type.Name == nameof(YetaWFEndpoints)) continue; // ignore base class
                    try {
                        MethodInfo? meth = type.GetMethod("RegisterEndpoints", BindingFlags.Static|BindingFlags.Public, new Type[] { typeof(IEndpointRouteBuilder), typeof(Package), typeof(string) });
                        if (meth is null) throw new InternalError($"RegisterEndpoints method not found on endpoint type {type.FullName}");
                        Package? package = Package.TryGetPackageFromType(type);
                        if (package is null) throw new InternalError($"RegisterEndpoints couldn't determine package for type {type.FullName}");
                        meth.Invoke(null, new object?[] { endpoints, package, package.AreaName });
                    } catch (Exception exc) {
                        Logging.AddErrorLog($"{nameof(RegisterPackages)} RegisterEndpoints failed in class {type.FullName} failed.", exc);
                        throw;
                    }
                }
            }
            Logging.AddLog($"Processing {nameof(RegisterPackages)} Ended");
        }

        private static List<Type> GetAreaRegistrationTypes() {
            return Package.GetClassesInPackages<AreaRegistrationBase>();
        }

        private static List<Type> GetEndpointRegistrationTypes() {
            return Package.GetClassesInPackages<YetaWFEndpoints>();
        }
    }
}

