/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

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
        public AreaRegistrationBase() { }

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

