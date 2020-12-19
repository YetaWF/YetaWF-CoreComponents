/* Copyright © 2021 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Authentication;
using System;
using System.Collections.Generic;
using YetaWF.Core.Packages;

namespace YetaWF.Core.Support.Services {

    public interface IDynamicAuthentication {
        void AddDynamicAuthentication(AuthenticationBuilder authBuilder);
    }

    public static class DynamicAuthentication {
        public static void Setup(AuthenticationBuilder authBuilder) {
            List<Type> types = Package.GetClassesInPackages<IDynamicAuthentication>();
            foreach (Type type in types) {
                object? o = Activator.CreateInstance(type) !;
                IDynamicAuthentication? dynAuth = o as IDynamicAuthentication;
                dynAuth!.AddDynamicAuthentication(authBuilder);
            }
        }
    }

    public static class DynamicAuthenticationExtender {
        public static void AddDynamicAuthentication(this AuthenticationBuilder authBuilder) {
            DynamicAuthentication.Setup(authBuilder);
        }
    }

}
