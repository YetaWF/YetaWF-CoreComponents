/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

#if MVC6

using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using YetaWF.Core.Support;

namespace YetaWF2.Support {

    public class DataProtectionKeyRepository : IXmlRepository {

        public IReadOnlyCollection<XElement> GetAllElements() {
            string s = WebConfigHelper.GetValue<string>("DataProtection", "List", null, false);
            if (s == null) return new List<XElement>();
            return YetaWFManager.JsonDeserialize<List<XElement>>(s);
        }

        public void StoreElement(XElement element, string friendlyName) {
            List<XElement> list = new List<XElement>(GetAllElements());
            XElement elem = (from l in list where l.Name == friendlyName select l).FirstOrDefault();
            if (elem != null)
                list.Remove(elem);
            list.Add(element);

            string s = YetaWFManager.JsonSerialize(list);
            WebConfigHelper.SetValue<string>("DataProtection", "List", s, false);
            WebConfigHelper.SaveAsync().Wait();// oh well
        }
    }

    public static class DataProtectionKeyExtensions {
        public static IDataProtectionBuilder PersistKeysToAppSettings(this IDataProtectionBuilder builder, IServiceCollection services = null) {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            return builder.Use(ServiceDescriptor.Scoped<IXmlRepository, DataProtectionKeyRepository>());
        }
        public static IDataProtectionBuilder Use(this IDataProtectionBuilder builder, ServiceDescriptor descriptor) {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (descriptor == null) throw new ArgumentNullException(nameof(descriptor));

            for (int i = builder.Services.Count - 1; i >= 0; i--) {
                if (builder.Services[i]?.ServiceType == descriptor.ServiceType)
                    builder.Services.RemoveAt(i);
            }
            builder.Services.Add(descriptor);
            return builder;
        }
    }
}

#else
#endif
