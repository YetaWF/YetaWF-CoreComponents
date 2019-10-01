/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;
using System.Reflection;

namespace YetaWF.Core.Support {

    /// <summary>
    /// Manages assemblies.
    /// </summary>
    public static class Assemblies {

        private static Dictionary<string, Assembly> LoadedAssemblies = new Dictionary<string, Assembly>();

        /// <summary>
        /// Loads an assembly.
        /// </summary>
        /// <param name="asmName">The name of the assembly.</param>
        /// <returns>An instance of the requested assembly.</returns>
        /// <remarks>Assemblies are cached for performance.</remarks>
        public static Assembly Load(string asmName) {
            Assembly assembly;
            if (LoadedAssemblies.TryGetValue(asmName.ToLower(), out assembly))
                return assembly;
            assembly = System.Reflection.Assembly.Load(asmName);
            if (asmName != null) {
                try {
                    LoadedAssemblies.Add(asmName.ToLower(), assembly);
                } catch (System.Exception) { }// ignore if already added
            }
            return assembly;
        }

        public static void AddLoaded(Assembly assembly) {
            string name = System.IO.Path.GetFileNameWithoutExtension(assembly.ManifestModule.Name);
            name = name.ToLower();
            if (!LoadedAssemblies.ContainsKey(name))
                LoadedAssemblies.Add(name.ToLower(), assembly);
        }
    }
}
