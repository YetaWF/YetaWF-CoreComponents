/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using YetaWF.Core.Models;
using YetaWF.Core.Pages;

namespace YetaWF.Core.Search {

    /// <summary>
    /// Defines the interface used by pages/modules to collect search terms for Urls.
    /// </summary>
    public interface ISearchWords {
        /// <summary>
        /// Verifies that the specified page is eligible for search terms, based on allowed access by anonymous users, authenticated users and additional page attributes.
        /// </summary>
        /// <returns>true if the page is eligible, false otherwise.</returns>
        bool WantPage(PageDefinition page);
        /// <summary>
        /// Prepares to add new search terms for the specified Url.
        /// </summary>
        /// <returns>true if the Url is eligible, false otherwise.</returns>
        bool SetUrl(string url, PageDefinition.PageSecurityType pageSecurity, MultiString title, MultiString summary, DateTime dateCreated, DateTime? dateUpdated, bool allowAnonymous, bool allowUser);
        /// <summary>
        /// Adds the specified data as search terms for the current Url.
        /// </summary>
        void AddContent(MultiString content);
        /// <summary>
        /// Adds title keywords. SetUrl automatically adds the provided title as search terms.
        /// </summary>
        void AddTitle(MultiString content);
        /// <summary>
        /// Adds keywords as search terms.
        /// </summary>
        /// <remarks></remarks>
        void AddKeywords(MultiString content);
        /// <summary>
        /// Adds all public strings and MultiString properties with get/set accessors as search terms for the current Url.
        /// </summary>
        void AddObjectContents(object searchObject);
        /// <summary>
        /// Saves the search terms processed for the current Url.
        /// </summary>
        /// <remarks>Once Save() is called, a new Url can be processed after calls to WantPage/SetUrl.</remarks>
        void Save();
    }
}
