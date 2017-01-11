/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;
using System.Reflection;

namespace YetaWF.Core.Support {

    public static class Assemblies {

        private static Dictionary<string, Assembly> LoadedAssemblies = new Dictionary<string, Assembly>();

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
    }
}
