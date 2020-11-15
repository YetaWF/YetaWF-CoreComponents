/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;
using YetaWF.Core.Support;

namespace YetaWF2.LetsEncrypt {

    /// <summary>
    /// Adds LetsEncrypt support based on whether the assembly YetaWF.Core.LetsEncrypt is available.
    /// </summary>
    public static class LetsEncrypt {

        private const string ASSEMBLY = "YetaWF.Core.LetsEncrypt";
        private const string TYPE = "YetaWF.Core.LetsEncrypt.LetsEncrypt";

        public static bool Enabled { get; private set; } = false;

        public static void AddLetsEncrypt(this IServiceCollection services) {
            Assembly? fluffyAssembly = LoadAssembly();
            if (fluffyAssembly == null)
                return;
            Type tp = fluffyAssembly.GetType(TYPE) !;
            dynamic inst = Activator.CreateInstance(tp) !;
            Enabled = inst.AddLetsEncrypt(services);
        }
        public static void UseLetsEncrypt(this IApplicationBuilder app) {
            Assembly? fluffyAssembly = LoadAssembly();
            if (fluffyAssembly == null)
                return;
            Type tp = fluffyAssembly.GetType(TYPE) !;
            dynamic inst = Activator.CreateInstance(tp) !;
            inst.UseLetsEncrypt(app);
        }

        private static Assembly? LoadAssembly() {
            Assembly? fluffyAssembly = null;
            string? asmName = WebConfigHelper.GetValue("LetsEncrypt", "Assembly", ASSEMBLY, Package: false);
            if (asmName != null)
                fluffyAssembly = Assemblies.Load(asmName, throwError: false) !;
            return fluffyAssembly;
        }
    }
}
