/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Reflection;
using YetaWF.Core.Support;
using Microsoft.Extensions.DependencyInjection;

namespace YetaWF.Core.Identity {

    public interface IIdentity {
        void SetupLoginProviders(IServiceCollection services);
        void Setup(IServiceCollection services);
    }

    public static class IdentityCreator {

        public static void Setup(IServiceCollection services) {
            object instance = LoadAssembly();
            IIdentity? identity = instance as IIdentity;
            if (identity != null)
                identity.Setup(services);
        }
        public static void SetupLoginProviders(IServiceCollection services) {
            object instance = LoadAssembly();
            IIdentity? identity = instance as IIdentity;
            if (identity != null)
                identity.SetupLoginProviders(services);
        }

        private static object LoadAssembly() {

            string? assembly = WebConfigHelper.GetValue<string>("Identity", "Assembly");
            string? type = WebConfigHelper.GetValue<string>("Identity", "Type");

            if (string.IsNullOrWhiteSpace(assembly) || string.IsNullOrWhiteSpace(type))
                throw new InternalError("The Identity provider assembly is not defined");

            // load the assembly/type implementing identity
            Type? tp = null;
            try {
                Assembly? asm = Assemblies.Load(assembly);
                tp = asm!.GetType(type) !;
            } catch (Exception) { }

            // create an instance of the class implementing identity
            object instance;
            try {
                instance = Activator.CreateInstance(tp!) !;
            } catch (Exception exc) {
                throw new InternalError("The Identity provider assembly cannot be loaded - ", exc);
            }

            return instance;
        }
    }
}
