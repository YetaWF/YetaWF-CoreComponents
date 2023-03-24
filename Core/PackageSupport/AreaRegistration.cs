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

namespace YetaWF.Core.PackageSupport
{

    /// <summary>
    /// Used for area registration.
    /// </summary>
    /// <remarks>
    /// An instance of this class is instantiated and initialized during application startup in order to define the MVC area used by a package.
    /// Each package defines its own MVC area. The name is derived from the YetaWF.PackageAttributes.PackageAttribute (for the domain portion) 
    /// and the <see cref="AssemblyProductAttribute"/> (for the product name),
    /// defined in the package's AssemblyInfo.cs source file.
    ///
    /// The area name is the concatenation of the domain, followed by an underscore and the product name (e.g., YetaWF_Text).
    ///
    /// Applications can reference the current package using the static CurrentPackage property.
    ///
    /// Applications do not instantiate this class.
    /// </remarks>    
    public static class AreaRegistrationBase
    {

        /// <summary>
        /// Used to register all package areas and endpoints.
        /// </summary>
        public static void RegisterPackages(IEndpointRouteBuilder? endpoints = null)
        {
            Logging.AddLog($"Processing {nameof(RegisterPackages)}");
            // Areas
            List<Type> types = GetRegisterablePackages();
            foreach (Type type in types)
            {
                try
                {
                    // Set CurrentPackage
                    Logging.AddLog($"{nameof(RegisterPackages)} class \'{0}\' found", type.FullName!);
                    Package? package = Package.TryGetPackageFromType(type);
                    if (package is null) throw new InternalError($"{nameof(RegisterPackages)} couldn't determine package for type {type.FullName}");
                    PropertyInfo? prop = type.GetProperty("CurrentPackage", BindingFlags.Static | BindingFlags.Public);
                    if (prop is null) throw new InternalError($"CurrentPackage property not found on area type {type.FullName}");
                    prop.SetValue(null, package);
                    // Register area
                    //if (endpoints != null)
                    //    RegisterArea(package, endpoints);
                }
                catch (Exception exc)
                {
                    Logging.AddErrorLog($"{nameof(RegisterPackages)} class {0} failed.", type.FullName!, exc);
                    throw;
                }
            }
            // Endpoints
            if (endpoints != null)
            {
                types = GetEndpointRegistrationTypes();
                foreach (Type type in types)
                {
                    if (type.Name == nameof(YetaWFEndpoints)) continue; // ignore base class
                    try
                    {
                        MethodInfo? meth = type.GetMethod("RegisterEndpoints", BindingFlags.Static | BindingFlags.Public, new Type[] { typeof(IEndpointRouteBuilder), typeof(Package), typeof(string) });
                        if (meth is null) throw new InternalError($"RegisterEndpoints method not found on endpoint type {type.FullName}");
                        Package? package = Package.TryGetPackageFromType(type);
                        if (package is null) throw new InternalError($"RegisterEndpoints couldn't determine package for type {type.FullName}");
                        meth.Invoke(null, new object?[] { endpoints, package, package.AreaName });
                    }
                    catch (Exception exc)
                    {
                        Logging.AddErrorLog($"{nameof(RegisterPackages)} RegisterEndpoints failed in class {type.FullName} failed.", exc);
                        throw;
                    }
                }
            }
            Logging.AddLog($"Processing {nameof(RegisterPackages)} Ended");
        }

        private static List<Type> GetEndpointRegistrationTypes()
        {
            return Package.GetClassesInPackages<YetaWFEndpoints>();
        }

        /// <summary>
        /// Return a list of all packages that can be registered
        /// </summary>
        private static List<Type> GetRegisterablePackages()
        {
            List<Type> list = new List<Type>();
            List<Package> packages = Package.GetAvailablePackages();

            foreach (Package package in packages)
            {
                Type?[] typesInAsm;
                try
                {
                    typesInAsm = package.PackageAssembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    typesInAsm = ex.Types;
                }
                foreach (Type? t in typesInAsm)
                {
                    if (t != null && t.Name == nameof(AreaRegistration))
                        list.Add(t);
                }
            }
            return list;
        }
    }
}

