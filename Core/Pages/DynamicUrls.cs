/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YetaWF.Core.Packages;
using YetaWF.Core.Search;

namespace YetaWF.Core.Pages {

    public class DynamicUrlsImpl {

        public List<Type> GetDynamicUrlTypes() {
            return Package.GetClassesInPackages<ISearchDynamicUrls>();
        }
    }

    public interface ISearchDynamicUrls
    {
        /// <summary>
        /// Used by Search to extract keywords from dynamically generated pages.
        /// </summary>
        /// <param name="addTermsForPage"></param>
        Task KeywordsForDynamicUrlsAsync(ISearchWords searchWords);
    }
    public interface ISiteMapDynamicUrls
    {
        /// <summary>
        ///  Used to discover dynamic Urls to build a site map.
        /// </summary>
        Task FindDynamicUrlsAsync(Action<PageDefinition, string, DateTime?, PageDefinition.SiteMapPriorityEnum, PageDefinition.ChangeFrequencyEnum, object> addDynamicUrl,
                Func<PageDefinition, bool> validForSiteMap);
    }
}
