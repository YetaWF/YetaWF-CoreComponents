/* Copyright © 2022 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Mvc.ApplicationParts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using YetaWF.Core.Packages;
using YetaWF.Core.Support;

namespace YetaWF2.Support {
    public static class YetaWFApplicationPartManager {

        public static bool Initialized { get; private set; } = false;

        // ASP.NET Core 3.0 no longer returns all referenced assemblies with System.AppDomain.CurrentDomain.GetAssemblies();
        // So now we locate all "missing" assemblies like this.
        private static List<Assembly> FindExtraAssemblies(List<Assembly> assemblies, string baseDirectory) {

            List<Assembly> list = new List<Assembly>();

            // Get all assemblies that are already loaded
            List<Assembly> preloadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
            foreach (Assembly preloadedAssembly in preloadedAssemblies) {
                Assembly? found = (from Assembly a in assemblies where a.FullName == preloadedAssembly.FullName select a).FirstOrDefault();
                if (found == null) {
                    Package package = new Package(preloadedAssembly);
                    if (package.IsValid) {
                        Assemblies.AddLoaded(preloadedAssembly);// add the assembly to our own assembly cache
                        assemblies.Add(preloadedAssembly);// add to known list
                        list.Add(preloadedAssembly);// this assembly wasn't in the list of assembly for which we have ApplicationParts
                    }
                }
            }

            // Find extra assemblies that aren't loaded yet
            string[] files = Directory.GetFiles(baseDirectory, "*.dll");
            foreach (string file in files) {
                Assembly? found = (from Assembly a in assemblies where a.ManifestModule.FullyQualifiedName == file select a).FirstOrDefault();
                if (found == null) {
                    Assembly? newAssembly = null;
                    if (!file.EndsWith("\\libuv.dll")) {// avoid exception spam
                        try {
                            newAssembly = Assembly.LoadFrom(file);
                        } catch (Exception) { }
                    }
                    if (newAssembly != null) {
                        Package package = new Package(newAssembly);
                        if (package.IsValid) {
                            Assemblies.AddLoaded(newAssembly);// add the assembly to our own assembly cache
                            list.Add(newAssembly);// this assembly wasn't in the list of assembly for which we have ApplicationParts
                        }
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// Add all assemblies that we need (packages). AppDomain.CurrentDomain.GetAssemblies() used to return the full list (pre 3.0) but it no longer does.
        /// </summary>
        /// <param name="partManager">The ApplicationPartManager instance (singleton).</param>
        public static void AddAssemblies(ApplicationPartManager partManager) {

            List<Assembly> assemblies = (from Microsoft.AspNetCore.Mvc.ApplicationParts.AssemblyPart p in partManager.ApplicationParts select p.Assembly).ToList();
            List<Assembly> extraAsms = FindExtraAssemblies(assemblies, AppDomain.CurrentDomain.BaseDirectory);

            //List<string> debug = (from e in extraAsms orderby e.FullName select e.FullName).ToList();

            foreach (Assembly asm in extraAsms) {
                var partFactory = ApplicationPartFactory.GetApplicationPartFactory(asm);
                foreach (var applicationPart in partFactory.GetApplicationParts(asm)) {
                    partManager.ApplicationParts.Add(applicationPart);
                }
            }

            YetaWFApplicationPartManager.Initialized = true;
        }
    }
}

