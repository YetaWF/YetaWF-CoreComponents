/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using YetaWF.Core.Packages;
using YetaWF.Core.Search;

namespace YetaWF.Core.Pages {

    public interface ISearchDynamicUrls {
        /// <summary>
        /// Used by Search to extract keywords from dynamically generated pages.
        /// </summary>
        /// <param name="addTermsForPage"></param>
        void KeywordsForDynamicUrls(ISearchWords searchWords);
    }
    public interface ISiteMapDynamicUrls {
        /// <summary>
        ///  Used to discover dynamic Urls to build a site map.
        /// </summary>
        void FindDynamicUrls(Action<PageDefinition, string, DateTime?, PageDefinition.SiteMapPriorityEnum, PageDefinition.ChangeFrequencyEnum> addDynamicUrl,
                Func<PageDefinition, bool> validForSiteMap);
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
            return typeof(ISearchDynamicUrls).IsAssignableFrom(type) || typeof(ISiteMapDynamicUrls).IsAssignableFrom(type);
        }
        private bool TypeIsPublicClass(Type type) {
            return (type != null && type.IsPublic && type.IsClass && !type.IsAbstract && !type.IsGenericType);
        }
    }
}
