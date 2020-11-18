/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

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
    /// Keeps track of static pages.
    /// </summary>
    public class StaticPageManager : IInitializeApplicationStartupFirstNodeOnly {

        private const string StaticFolder = "YetaWF_StaticPages";

        private YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        private SiteEntry Site { get; set; } = null!;

        /// <summary>
        /// Called when the first node of a multi-instance site is starting up.
        /// </summary>
        public async Task InitializeFirstNodeStartupAsync() {
            if (!YetaWFManager.IsBatchMode && !YetaWFManager.IsServiceMode)
                await RemoveAllPagesInternalAsync();
        }

        public enum PageEntryEnum {
            [EnumDescription("File", "Static pages cached using a local file")]
            File = 0,
            [EnumDescription("Memory", "Static pages cached in memory (not available with distributed caching - web farm/garden)")]
            Memory = 1,
        }
        public class SiteEntry {
            public int SiteIdentity { get; set; }
            public SerializableDictionary<string, PageEntry> StaticPages { get; set; } = null!;
        }
        public class PageEntry {
            public string LocalUrl { get; set; } = null!;
            public PageEntryEnum StorageType { get; set; }
            public DateTime LastUpdate { get; set; }
            public string? Content { get; set; }
            public string? ContentPopup { get; set; }
            public string? ContentHttps { get; set; }
            public string? ContentPopupHttps { get; set; }
            public string? FileName { get; set; }
            public string? FileNamePopup { get; set; }
            public string? FileNameHttps { get; set; }
            public string? FileNamePopupHttps { get; set; }
        }
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

        public async Task<List<PageEntry>> GetSiteStaticPagesAsync() {
            using (ILockObject staticLock = await YetaWF.Core.IO.Caching.LockProvider.LockResourceAsync(STATICPAGESKEY)) {
                using (ICacheDataProvider cacheStaticDP = YetaWF.Core.IO.Caching.GetStaticCacheProvider()) {
                    List<SiteEntry> siteEntries = await InitSiteWithLockAsync(cacheStaticDP, staticLock);
                    List<PageEntry> list = new List<PageEntry>(Site.StaticPages.Values);
                    await staticLock.UnlockAsync();
                    return list;
                }
            }
        }
        private string GetScheme() {
            return Manager.HostSchemeUsed;
        }
        public async Task AddPageAsync(string localUrl, bool cache, string pageHtml, DateTime lastUpdated) {
            using (ILockObject staticLock = await YetaWF.Core.IO.Caching.LockProvider.LockResourceAsync(STATICPAGESKEY)) {
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
                    await staticLock.UnlockAsync();
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

        public class GetPageInfo {
            public string? FileContents { get; set; }
            public DateTime LastUpdate{ get; set; }
        }

        public async Task<GetPageInfo> GetPageAsync(string localUrl) {
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
        public async Task<bool> HavePageAsync(string localUrl) {
            using (ICacheDataProvider cacheStaticDP = YetaWF.Core.IO.Caching.GetStaticCacheProvider()) {
                List<SiteEntry> siteEntries = await InitSiteAsync(cacheStaticDP);
                string localUrlLower = localUrl.ToLower();
                return Site.StaticPages.TryGetValue(localUrlLower, out PageEntry? entry);
            }
        }
        public async Task RemovePageAsync(string localUrl) {
            using (ILockObject staticLock = await YetaWF.Core.IO.Caching.LockProvider.LockResourceAsync(STATICPAGESKEY)) {
                using (ICacheDataProvider cacheStaticDP = YetaWF.Core.IO.Caching.GetStaticCacheProvider()) {
                    List<SiteEntry> siteEntries = await InitSiteWithLockAsync(cacheStaticDP, staticLock);

                    string localUrlLower = localUrl.ToLower();

                    Site.StaticPages.Remove(localUrlLower);
                    await cacheStaticDP.AddAsync(STATICPAGESKEY, siteEntries);

                    await RemovePageSet(localUrl);

                    await staticLock.UnlockAsync();
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

        public async Task RemovePagesAsync(List<PageDefinition> pages) {
            if (pages == null) return;
            using (ICacheDataProvider cacheStaticDP = YetaWF.Core.IO.Caching.GetStaticCacheProvider()) {
                using (ILockObject staticLock = await YetaWF.Core.IO.Caching.LockProvider.LockResourceAsync(STATICPAGESKEY)) {
                    List<SiteEntry> siteEntries = await InitSiteWithLockAsync(cacheStaticDP, staticLock);
                    foreach (PageDefinition page in pages) {
                        string localUrlLower = page.Url.ToLower();
                        await RemovePageSet(page.Url);
                        Site.StaticPages.Remove(localUrlLower);
                    }
                    await cacheStaticDP.AddAsync(STATICPAGESKEY, siteEntries);

                    await staticLock.UnlockAsync();
                }
            }
        }
        public async Task RemoveAllPagesAsync() {
            using (ICacheDataProvider cacheStaticDP = YetaWF.Core.IO.Caching.GetStaticCacheProvider()) {
                using (ILockObject staticLock = await YetaWF.Core.IO.Caching.LockProvider.LockResourceAsync(STATICPAGESKEY)) {
                    List<SiteEntry> siteEntries = await InitSiteWithLockAsync(cacheStaticDP, staticLock);
                    Site.StaticPages = new SerializableDictionary<string, StaticPages.StaticPageManager.PageEntry>();
                    await cacheStaticDP.AddAsync(STATICPAGESKEY, siteEntries);
                    await RemoveAllPagesInternalAsync();
                    await staticLock.UnlockAsync();
                }
            }
        }
        private async Task RemoveAllPagesInternalAsync() {
            Logging.AddLog("Removing/creating bundle folder");
            string folder = Path.Combine(YetaWFManager.RootSitesFolder, StaticFolder);
            await FileSystem.FileSystemProvider.DeleteDirectoryAsync(folder);
            await FileSystem.FileSystemProvider.CreateDirectoryAsync(folder);
            // create a don't deploy marker
            await FileSystem.FileSystemProvider.WriteAllTextAsync(Path.Combine(folder, Globals.DontDeployMarker), "");
        }
    }
}
