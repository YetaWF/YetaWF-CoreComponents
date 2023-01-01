/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YetaWF.Core.IO;
using YetaWF.Core.Log;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Pages;
using YetaWF.Core.Serializers;

namespace YetaWF.Core.Support.StaticPages {

    /// <summary>
    /// An instance of this class keeps track of all static pages which are saved when a page is accessed for the first time.
    /// </summary>
    /// <remarks>For more information about static pages see <see href="https://YetaWF.com/Documentation/YetaWF/Topic/g_doc_staticpages"/>.</remarks>
    public class StaticPageManager : IInitializeApplicationStartupFirstNodeOnly {

        private const string StaticFolder = "YetaWF_StaticPages";

        private YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        private SiteEntry Site { get; set; } = null!;

        /// <summary>
        /// Called when the first node of a multi-instance site is starting up. For internal framework use only.
        /// </summary>
        public async Task InitializeFirstNodeStartupAsync() {
            if (!YetaWFManager.IsBatchMode && !YetaWFManager.IsServiceMode)
                await RemoveAllPagesInternalAsync();
        }

        /// <summary>
        /// Defines the method with which the static page is stored.
        /// </summary>
        public enum PageEntryEnum {
            /// <summary>
            /// Static page cached using a local file.
            /// </summary>
            [EnumDescription("File", "Static page cached using a local file")]
            File = 0,
            /// <summary>
            /// Static page cached in memory (not available with distributed caching - web farm/garden)
            /// </summary>
            [EnumDescription("Memory", "Static page cached in memory (not available with distributed caching - web farm/garden)")]
            Memory = 1,
        }
        internal class SiteEntry {
            public int SiteIdentity { get; set; }
            public SerializableDictionary<string, PageEntry> StaticPages { get; set; } = null!;
        }

        /// <summary>
        /// Describes a page that is saved as a static page.
        /// </summary>
        public class PageEntry {
            /// <summary>
            /// The page URL saved as a static page.
            /// </summary>
            public string LocalUrl { get; set; } = null!;
            /// <summary>
            /// Defines how the page is saved.
            /// </summary>
            public PageEntryEnum StorageType { get; set; }
            /// <summary>
            /// Defines when the saved page was last updated.
            /// </summary>
            public DateTime LastUpdate { get; set; }
            /// <summary>
            /// The static page content (using http://).
            /// </summary>
            public string? Content { get; set; }
            /// <summary>
            /// The static page content (using http://) when used in a popup window.
            /// </summary>
            public string? ContentPopup { get; set; }
            /// <summary>
            /// The static page content (using https://).
            /// </summary>
            public string? ContentHttps { get; set; }
            /// <summary>
            /// The static page content (using https://) when used in a popup window.
            /// </summary>
            public string? ContentPopupHttps { get; set; }
            /// <summary>
            /// The file where the contents are stored (using http://).
            /// </summary>
            public string? FileName { get; set; }
            /// <summary>
            /// The file where the contents are stored (using http://) when used in a popup window.
            /// </summary>
            public string? FileNamePopup { get; set; }
            /// <summary>
            /// The file where the contents are stored (using https://).
            /// </summary>
            public string? FileNameHttps { get; set; }
            /// <summary>
            /// The file where the contents are stored (using https://) when used in a popup window.
            /// </summary>
            public string? FileNamePopupHttps { get; set; }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public StaticPageManager() { }

        string STATICPAGESKEY { get { return $"__StaticPages_{YetaWFManager.Manager.CurrentSite.Identity}"; } }

        private Task<List<SiteEntry>> InitSiteWithLockAsync(ICacheDataProvider cacheStaticDP, ILockObject staticLock) {
            return InitSiteAsync(cacheStaticDP);
        }
        private async Task<List<SiteEntry>> InitSiteAsync(ICacheDataProvider cacheStaticDP) {
            SerializableList<SiteEntry> siteEntries;
            GetObjectInfo<SerializableList<SiteEntry>> info = await cacheStaticDP.GetAsync<SerializableList<SiteEntry>>(STATICPAGESKEY);
            if (info.Success) {
                siteEntries = info.RequiredData;
            } else {
                siteEntries = new SerializableList<SiteEntry>();
            }
            SiteEntry? siteEntry = (from s in siteEntries where s.SiteIdentity == Manager.CurrentSite.Identity select s).FirstOrDefault();
            if (siteEntry == null) {
                siteEntry = new SiteEntry { SiteIdentity = Manager.CurrentSite.Identity, StaticPages = new SerializableDictionary<string, StaticPages.StaticPageManager.PageEntry>() };
                siteEntries.Add(siteEntry);
            }
            Site = siteEntry;
            return siteEntries;
        }

        /// <summary>
        /// Retrieves a list of all static pages.
        /// </summary>
        /// <returns>A list of all currently saved static pages.</returns>
        public async Task<List<PageEntry>> GetSiteStaticPagesAsync() {
            await using (ILockObject staticLock = await YetaWF.Core.IO.Caching.LockProvider.LockResourceAsync(STATICPAGESKEY)) {
                using (ICacheDataProvider cacheStaticDP = YetaWF.Core.IO.Caching.GetStaticCacheProvider()) {
                    List<SiteEntry> siteEntries = await InitSiteWithLockAsync(cacheStaticDP, staticLock);
                    List<PageEntry> list = new List<PageEntry>(Site.StaticPages.Values);
                    return list;
                }
            }
        }
        private string GetScheme() {
            return Manager.HostSchemeUsed;
        }
        internal async Task AddPageAsync(string localUrl, bool cache, string pageHtml, DateTime lastUpdated) {
            await using(ILockObject staticLock = await YetaWF.Core.IO.Caching.LockProvider.LockResourceAsync(STATICPAGESKEY)) {
                using (ICacheDataProvider cacheStaticDP = YetaWF.Core.IO.Caching.GetStaticCacheProvider()) {
                    List<SiteEntry> siteEntries = await InitSiteWithLockAsync(cacheStaticDP, staticLock);

                    string localUrlLower = localUrl.ToLower();

                    string folder = Path.Combine(YetaWFManager.RootSitesFolder, StaticFolder, YetaWFManager.Manager.CurrentSite.Identity.ToString());
                    await FileSystem.FileSystemProvider.CreateDirectoryAsync(folder);

                    string tempFile;
                    if (GetScheme() == "https") {
                        if (Manager.IsInPopup) {
                            tempFile = "https_popup#";
                        } else {
                            tempFile = "https#";
                        }
                    } else {
                        if (Manager.IsInPopup) {
                            tempFile = "http_popup#";
                        } else {
                            tempFile = "http#";
                        }
                    }
                    tempFile = Path.Combine(folder, tempFile + FileSystem.FileSystemProvider.MakeValidDataFileName(localUrl));

                    if (!Site.StaticPages.TryGetValue(localUrlLower, out PageEntry? entry)) {
                        entry = new StaticPages.StaticPageManager.PageEntry {
                            LocalUrl = localUrl,
                            LastUpdate = lastUpdated,
                        };
                        Site.StaticPages.Add(localUrlLower, entry);
                    }
                    if (cache && !YetaWF.Core.Support.Startup.MultiInstance) {
                        entry.StorageType = PageEntryEnum.Memory;
                        SetContents(entry, pageHtml);
                    } else {
                        entry.StorageType = PageEntryEnum.File;
                        SetContents(entry, null);
                    }
                    SetFileName(entry, tempFile);

                    // save the file image
                    await FileSystem.FileSystemProvider.WriteAllTextAsync(tempFile, pageHtml);

                    await cacheStaticDP.AddAsync(STATICPAGESKEY, siteEntries);
                }
            }
        }

        private void SetFileName(PageEntry entry, string tempFile) {
            if (GetScheme() == "https") {
                if (Manager.IsInPopup) {
                    entry.FileNamePopupHttps = tempFile;
                } else {
                    entry.FileNameHttps = tempFile;
                }
            } else {
                if (Manager.IsInPopup) {
                    entry.FileNamePopup = tempFile;
                } else {
                    entry.FileName = tempFile;
                }
            }
        }
        private void SetContents(PageEntry entry, string? pageHtml) {
            if (GetScheme() == "https") {
                if (Manager.IsInPopup) {
                    entry.ContentPopupHttps = pageHtml;
                } else {
                    entry.ContentHttps = pageHtml;
                }
            } else {
                if (Manager.IsInPopup) {
                    entry.ContentPopup = pageHtml;
                } else {
                    entry.Content = pageHtml;
                }
            }
        }

        internal class GetPageInfo {
            public string? FileContents { get; set; }
            public DateTime LastUpdate{ get; set; }
        }

        internal async Task<GetPageInfo> GetPageAsync(string localUrl) {
            using (ICacheDataProvider cacheStaticDP = YetaWF.Core.IO.Caching.GetStaticCacheProvider()) {
                List<SiteEntry> siteEntries = await InitSiteAsync(cacheStaticDP);

                DateTime lastUpdate = DateTime.MinValue;
                string localUrlLower = localUrl.ToLower();
                if (Site.StaticPages.TryGetValue(localUrlLower, out PageEntry? entry)) {
                    lastUpdate = entry.LastUpdate;
                    if (entry.StorageType == PageEntryEnum.Memory) {
                        if (GetScheme() == "https") {
                            if (Manager.IsInPopup) {
                                return new GetPageInfo {
                                    FileContents = entry.ContentPopupHttps,
                                    LastUpdate = lastUpdate,
                                };
                            } else {
                                return new GetPageInfo {
                                    FileContents = entry.ContentHttps,
                                    LastUpdate = lastUpdate,
                                };
                            }
                        } else {
                            if (Manager.IsInPopup) {
                                return new GetPageInfo {
                                    FileContents = entry.ContentPopup,
                                    LastUpdate = lastUpdate,
                                };
                            } else {
                                return new GetPageInfo {
                                    FileContents = entry.Content,
                                    LastUpdate = lastUpdate,
                                };
                            }
                        }
                    } else /*if (entry.StorageType == PageEntryEnum.File)*/ {
                        string? tempFile = null;
                        if (GetScheme() == "https") {
                            if (Manager.IsInPopup) {
                                tempFile = entry.FileNamePopupHttps;
                            } else {
                                tempFile = entry.FileNameHttps;
                            }
                        } else {
                            if (Manager.IsInPopup) {
                                tempFile = entry.FileNamePopup;
                            } else {
                                tempFile = entry.FileName;
                            }
                        }
                        if (tempFile != null) {
                            try {
                                return new GetPageInfo {
                                    FileContents = await FileSystem.FileSystemProvider.ReadAllTextAsync(tempFile),
                                    LastUpdate = lastUpdate,
                                };
                            } catch (System.Exception) {
                                return new GetPageInfo {
                                    FileContents = null,
                                    LastUpdate = lastUpdate,
                                };
                            }
                        }
                    }
                }
                return new GetPageInfo {
                    FileContents = null,
                    LastUpdate = lastUpdate,
                };
            }
        }

        //public async Task<bool> HavePageAsync(string localUrl) {
        //    using (ICacheDataProvider cacheStaticDP = YetaWF.Core.IO.Caching.GetStaticCacheProvider()) {
        //        List<SiteEntry> siteEntries = await InitSiteAsync(cacheStaticDP);
        //        string localUrlLower = localUrl.ToLower();
        //        return Site.StaticPages.TryGetValue(localUrlLower, out PageEntry? entry);
        //    }
        //}

        /// <summary>
        /// Removes a static page.
        /// </summary>
        /// <param name="localUrl">The page whose saved static page is to be removed.</param>
        /// <remarks>The next time the page is accessed, it will be saved again as a static page.</remarks>
        public async Task RemovePageAsync(string localUrl) {
            await using (ILockObject staticLock = await YetaWF.Core.IO.Caching.LockProvider.LockResourceAsync(STATICPAGESKEY)) {
                using (ICacheDataProvider cacheStaticDP = YetaWF.Core.IO.Caching.GetStaticCacheProvider()) {
                    List<SiteEntry> siteEntries = await InitSiteWithLockAsync(cacheStaticDP, staticLock);

                    string localUrlLower = localUrl.ToLower();

                    Site.StaticPages.Remove(localUrlLower);
                    await cacheStaticDP.AddAsync(STATICPAGESKEY, siteEntries);

                    await RemovePageSet(localUrl);
                }
            }
        }

        private async Task RemovePageSet(string localUrl) {
            string siteFolder = Path.Combine(YetaWFManager.RootSitesFolder, StaticFolder, YetaWFManager.Manager.CurrentSite.Identity.ToString());
            string tempFile = Path.Combine(siteFolder, "http#" + FileSystem.FileSystemProvider.MakeValidDataFileName(localUrl));
            if (await FileSystem.FileSystemProvider.FileExistsAsync(tempFile)) await FileSystem.FileSystemProvider.DeleteFileAsync(tempFile);
            tempFile = Path.Combine(siteFolder, "https#" + FileSystem.FileSystemProvider.MakeValidDataFileName(localUrl));
            if (await FileSystem.FileSystemProvider.FileExistsAsync(tempFile)) await FileSystem.FileSystemProvider.DeleteFileAsync(tempFile);
            tempFile = Path.Combine(siteFolder, "http_popup#" + FileSystem.FileSystemProvider.MakeValidDataFileName(localUrl));
            if (await FileSystem.FileSystemProvider.FileExistsAsync(tempFile)) await FileSystem.FileSystemProvider.DeleteFileAsync(tempFile);
            tempFile = Path.Combine(siteFolder, "https_popup#" + FileSystem.FileSystemProvider.MakeValidDataFileName(localUrl));
            if (await FileSystem.FileSystemProvider.FileExistsAsync(tempFile)) await FileSystem.FileSystemProvider.DeleteFileAsync(tempFile);
        }

        internal async Task RemovePagesAsync(List<PageDefinition> pages) {
            if (pages == null) return;
            using (ICacheDataProvider cacheStaticDP = YetaWF.Core.IO.Caching.GetStaticCacheProvider()) {
                await using (ILockObject staticLock = await YetaWF.Core.IO.Caching.LockProvider.LockResourceAsync(STATICPAGESKEY)) {
                    List<SiteEntry> siteEntries = await InitSiteWithLockAsync(cacheStaticDP, staticLock);
                    foreach (PageDefinition page in pages) {
                        string localUrlLower = page.Url.ToLower();
                        await RemovePageSet(page.Url);
                        Site.StaticPages.Remove(localUrlLower);
                    }
                    await cacheStaticDP.AddAsync(STATICPAGESKEY, siteEntries);
                }
            }
        }

        /// <summary>
        /// Removes all static pages.
        /// </summary>
        /// <remarks>The next time a page is accessed, it will be saved again as a static page.</remarks>
        public async Task RemoveAllPagesAsync() {
            using (ICacheDataProvider cacheStaticDP = YetaWF.Core.IO.Caching.GetStaticCacheProvider()) {
                await using (ILockObject staticLock = await YetaWF.Core.IO.Caching.LockProvider.LockResourceAsync(STATICPAGESKEY)) {
                    List<SiteEntry> siteEntries = await InitSiteWithLockAsync(cacheStaticDP, staticLock);
                    Site.StaticPages = new SerializableDictionary<string, StaticPages.StaticPageManager.PageEntry>();
                    await cacheStaticDP.AddAsync(STATICPAGESKEY, siteEntries);
                    await RemoveAllPagesInternalAsync();
                }
            }
        }
        internal async Task RemoveAllPagesInternalAsync() {
            Logging.AddLog("Removing/creating bundle folder");
            string folder = Path.Combine(YetaWFManager.RootSitesFolder, StaticFolder);
            await FileSystem.FileSystemProvider.DeleteDirectoryAsync(folder);
            await FileSystem.FileSystemProvider.CreateDirectoryAsync(folder);
            // create a don't deploy marker
            await FileSystem.FileSystemProvider.WriteAllTextAsync(Path.Combine(folder, Globals.DontDeployMarker), "");
        }
    }
}
