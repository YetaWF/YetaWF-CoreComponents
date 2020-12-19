/* Copyright © 2021 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

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
        Task KeywordsForDynamicUrlsAsync(ISearchWords searchWords);
    }

    public interface ISearchPageDynamicUrls {
        /// <summary>
        /// Used by Search to extract keywords from dynamically generated pages.
        /// </summary>
        Task KeywordsForDynamicUrlsAsync(PageDefinition page, ISearchWords searchWords);
    }

    public delegate Task AddDynamicUrlAsync(PageDefinition page, string url, DateTime? dateUpdated,
        PageDefinition.SiteMapPriorityEnum priority, PageDefinition.ChangeFrequencyEnum changeFrequency, object obj);

    public interface ISiteMapDynamicUrls {
        /// <summary>
        ///  Used to discover dynamic Urls to build a site map.
        /// </summary>
        Task FindDynamicUrlsAsync(AddDynamicUrlAsync addDynamicUrlAsync, Func<PageDefinition, bool> validForSiteMap);
    }

    public interface ISiteMapPageDynamicUrls {
        /// <summary>
        ///  Used to discover dynamic Urls to build a site map.
        /// </summary>
        Task FindDynamicUrlsAsync(PageDefinition page, AddDynamicUrlAsync addDynamicUrlAsync, Func<PageDefinition, bool> validForSiteMap);
    }
}
