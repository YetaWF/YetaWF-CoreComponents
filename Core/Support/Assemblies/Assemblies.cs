/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace YetaWF.Core.Support {

    /// <summary>
    /// Manages assemblies.
    /// </summary>
    public static class Assemblies {

        private static readonly Dictionary<string, Assembly> LoadedAssemblies = new Dictionary<string, Assembly>();

        /// <summary>
        /// Loads an assembly.
        /// </summary>
        /// <param name="asmName">The name of the assembly.</param>
        /// <param name="throwError">Defines whether an error is thrown if the assembly is not found.</param>
        /// <returns>An instance of the requested assembly.</returns>
        /// <remarks>Assemblies are cached for performance.</remarks>
        public static Assembly? Load(string asmName, bool throwError = true) {
            if (LoadedAssemblies.TryGetValue(asmName.ToLower(), out Assembly? assembly))
                return assembly;
            try {
                assembly = System.Reflection.Assembly.Load(asmName);
            } catch (System.Exception) {
                if (throwError)
                    throw;
                return null;
            }
            if (asmName != null) {
                try {
                    LoadedAssemblies.Add(asmName.ToLower(), assembly);
                } catch (System.Exception) { }// ignore if already added
            }
            return assembly;
        }

        /// <summary>
        /// Add an already loaded assembly to the list of loaded assemblies.
        /// </summary>
        /// <param name="assembly"></param>
        public static void AddLoaded(Assembly assembly) {
            string name = System.IO.Path.GetFileNameWithoutExtension(assembly.ManifestModule.Name);
            name = name.ToLower();
            try {
                LoadedAssemblies.Add(name.ToLower(), assembly);
            } catch (System.Exception) { }
        }

        /// <summary>
        /// Returns a list of currently loaded assemblies.
        /// </summary>
        /// <returns>Returns a list of currently loaded assemblies.</returns>
        public static List<Assembly> GetLoadedAssemblies() {
            return LoadedAssemblies.Values.ToList();
        }
    }
}
