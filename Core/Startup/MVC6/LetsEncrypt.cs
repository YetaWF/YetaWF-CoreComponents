/* Copyright �2020 Softel vdm, Inc.. - https://yetawf.com/Documentation/YetaWF/Licensing */

#if MVC6

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;
using YetaWF.Core.Support;

namespace YetaWF2.LetsEncrypt {

    /// <summary>
    /// Support adding LetsEncrypt support based on whether the assembly YetaWF.Core.LetsEncrypt is available.
    /// </summary>
    public static class LetsEncrypt {

        private const string ASSEMBLY = "YetaWF.Core.LetsEncrypt";
        private const string TYPE = "YetaWF.Core.LetsEncrypt.LetsEncrypt";

        public static void AddLetsEncrypt(this IServiceCollection services) {
            Assembly fluffyAssembly = LoadAssembly();
            if (fluffyAssembly == null)
                return;            
            Type tp = fluffyAssembly.GetType(TYPE);
            dynamic inst = Activator.CreateInstance(tp);
            inst.AddLetsEncrypt(services);
        }
        public static void UseLetsEncrypt(this IApplicationBuilder app) {
            Assembly fluffyAssembly = LoadAssembly();
            if (fluffyAssembly == null)
                return;
            Type tp = fluffyAssembly.GetType(TYPE);
            dynamic inst = Activator.CreateInstance(tp);
            inst.UseLetsEncrypt(app);
        }

        private static Assembly LoadAssembly() {
            string asmName = WebConfigHelper.GetValue("LetsEncrypt", "Assembly", ASSEMBLY, Package: false);
            Assembly fluffyAssembly = Assemblies.Load(asmName, throwError: false);
            return fluffyAssembly;
        }
    }
}

#else
#endif
