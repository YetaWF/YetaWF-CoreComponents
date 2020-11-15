/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

#nullable enable

using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using YetaWF.Core.Packages;

namespace YetaWF.Core.Support.Services {

    public interface IDynamicService {
        void AddService(IServiceCollection services);
    }

    public static class DynamicServices {
        public static void Setup(IServiceCollection services) {
            List<Type> types = Package.GetClassesInPackages<IDynamicService>();
            foreach (Type type in types) {
                object? o = Activator.CreateInstance(type);
                IDynamicService? dynServ = o as IDynamicService;
                dynServ!.AddService(services);
            }
        }
    }

    public static class DynamicServicesExtender {
        public static void AddDynamicServices(this IServiceCollection services) {
            DynamicServices.Setup(services);
        }
    }
}
