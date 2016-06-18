/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using YetaWF.Core.Packages;

namespace YetaWF.Core.Pages {

    public interface ISearchDynamicUrls {
        // this is used by Search to extract keywords from dynamically generated pages
        void KeywordsForDynamicUrls(Action<YetaWF.Core.Models.MultiString, PageDefinition, string, string, DateTime, DateTime?> addTermsForPage);
    }

    public class DynamicUrlsImpl {

        public List<Type> GetDynamicUrlTypes() {
            List<Type> moduleTypes = new List<Type>();

            foreach (Package package in Package.GetAvailablePackages()) {
                Assembly assembly = package.PackageAssembly;
                Type[] typesInAsm;
                try {
                    typesInAsm = assembly.GetTypes();
                } catch (ReflectionTypeLoadException ex) {
                    typesInAsm = ex.Types;
                }
                Type[] modTypes = typesInAsm.Where(type => IsDynamicUrlType(type)).ToArray<Type>();
                moduleTypes.AddRange(modTypes);
            }
            return moduleTypes;
        }
        private bool IsDynamicUrlType(Type type) {
            if (!TypeIsPublicClass(type))
                return false;
            return typeof(ISearchDynamicUrls).IsAssignableFrom(type);
        }
        private bool TypeIsPublicClass(Type type) {
            return (type != null && type.IsPublic && type.IsClass && !type.IsAbstract && !type.IsGenericType);
        }
    }
}
