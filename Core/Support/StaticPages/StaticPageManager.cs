/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using YetaWF.Core.IO;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Pages;

namespace YetaWF.Core.Support.StaticPages {

    /// <summary>
    /// Keeps track of static pages.
    /// </summary>
    public class StaticPageManager {

        private const string StaticFolder = "YetaWF_StaticPages";

        private YetaWFManager Manager { get; set; }
        private SiteEntry Site { get; set; }
        private static AsyncLock _lockObject = new AsyncLock();

        public enum PageEntryEnum {
            [EnumDescription("File", "Static pages cached using a local file")]
            File = 0,
            [EnumDescription("Memory", "Static pages cached in memory")]
            Memory = 1,
        }
        public class SiteEntry {
            public int SiteIdentity { get; set; }
            public Dictionary<string, PageEntry> StaticPages { get; set; }
        }
        public class PageEntry {
            public string LocalUrl { get; set; }
            public PageEntryEnum StorageType { get; set; }
            public DateTime LastUpdate { get; set; }
            public string Content { get; set; }
            public string ContentPopup { get; set; }
            public string ContentHttps { get; set; }
            public string ContentPopupHttps { get; set; }
            public string FileName { get; set; }
            public string FileNamePopup { get; set; }
            public string FileNameHttps { get; set; }
            public string FileNamePopupHttps { get; set; }
        }
        public StaticPageManager(YetaWFManager manager) {
            Manager = manager;
        }

        public static Dictionary<int, SiteEntry> Sites { get; private set; }

        private async Task InitSiteAsync() {
            using (await _lockObject.LockAsync()) {// short-term lock to sync static pages during startup
                if (Sites == null)
                    Sites = new Dictionary<int, SiteEntry>();
                if (!Sites.ContainsKey(Manager.CurrentSite.Identity)) {
                    await RemoveAllPagesInternalAsync();
                    string folder = Path.Combine(Manager.SiteFolder, StaticFolder);
                    await FileSystem.FileSystemProvider.CreateDirectoryAsync(folder);
                    // create a don't deploy marker
                    await FileSystem.FileSystemProvider.WriteAllTextAsync(Path.Combine(folder, Globals.DontDeployMarker), "");
                }
                SiteEntry site;
                if (!Sites.TryGetValue(Manager.CurrentSite.Identity, out site)) {
                    site = new SiteEntry { SiteIdentity = Manager.CurrentSite.Identity, StaticPages = new Dictionary<string, StaticPages.StaticPageManager.PageEntry>() };
                    Sites.Add(Manager.CurrentSite.Identity, site);
                }
                Site = site;
            }
        }

        public async Task<List<PageEntry>> GetSiteStaticPagesAsync() {
            await InitSiteAsync();
            List<PageEntry> list = new List<PageEntry>(Site.StaticPages.Values);
            return list;
        }
        private string GetScheme() {
#if MVC6
            return Manager.CurrentRequest.Scheme;
#else
            return Manager.CurrentRequest.Url.Scheme;
#endif
        }
        public async Task AddPageAsync(string localUrl, bool cache, string pageHtml, DateTime lastUpdated) {
            await InitSiteAsync();
            string localUrlLower = localUrl.ToLower();
            string folder = Path.Combine(Manager.SiteFolder, StaticFolder);

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
            tempFile = Path.Combine(folder, tempFile + FileData.MakeValidFileName(localUrl));
            using (await _lockObject.LockAsync()) {
                PageEntry entry;
                if (!Site.StaticPages.TryGetValue(localUrlLower, out entry)) {
                    entry = new StaticPages.StaticPageManager.PageEntry {
                        LocalUrl = localUrl,
                        LastUpdate = lastUpdated,
                    };
                    Site.StaticPages.Add(localUrlLower, entry);
                }
                if (cache) {
                    entry.StorageType = PageEntryEnum.Memory;
                    SetContents(entry, pageHtml);
                } else {
                    entry.StorageType = PageEntryEnum.File;
                    SetContents(entry, null);
                }
                SetFileName(entry, tempFile);
            }
            // save the file image
            await FileSystem.FileSystemProvider.WriteAllTextAsync(tempFile, pageHtml);
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

        private void SetContents(PageEntry entry, string pageHtml) {
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
            public string FileContents { get; set; }
            public DateTime LastUpdate{ get; set; }
        }

        public async Task<GetPageInfo> GetPageAsync(string localUrl) {
            await InitSiteAsync();
            DateTime lastUpdate = DateTime.MinValue;
            string localUrlLower = localUrl.ToLower();
            PageEntry entry = null;
            if (Site.StaticPages.TryGetValue(localUrlLower, out entry)) {
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
                    string tempFile = null;
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
            return new GetPageInfo {
                FileContents = null,
                LastUpdate = lastUpdate,
            };
        }
        public async Task<bool> HavePageAsync(string localUrl) {
            await InitSiteAsync();
            string localUrlLower = localUrl.ToLower();
            PageEntry entry = null;
            return Site.StaticPages.TryGetValue(localUrlLower, out entry);
        }
        public async Task RemovePageAsync(string localUrl) {
            await InitSiteAsync();
            string localUrlLower = localUrl.ToLower();
            using (await _lockObject.LockAsync()) {
                Site.StaticPages.Remove(localUrlLower);
                string tempFile = Path.Combine(Manager.SiteFolder, StaticFolder, "http#" + FileData.MakeValidFileName(localUrl));
                if (await FileSystem.FileSystemProvider.FileExistsAsync(tempFile)) await FileSystem.FileSystemProvider.DeleteFileAsync(tempFile);
                tempFile = Path.Combine(Manager.SiteFolder, StaticFolder, "https#" + FileData.MakeValidFileName(localUrl));
                if (await FileSystem.FileSystemProvider.FileExistsAsync(tempFile)) await FileSystem.FileSystemProvider.DeleteFileAsync(tempFile);
                tempFile = Path.Combine(Manager.SiteFolder, StaticFolder, "http_popup#" + FileData.MakeValidFileName(localUrl));
                if (await FileSystem.FileSystemProvider.FileExistsAsync(tempFile)) await FileSystem.FileSystemProvider.DeleteFileAsync(tempFile);
                tempFile = Path.Combine(Manager.SiteFolder, StaticFolder, "https_popup#" + FileData.MakeValidFileName(localUrl));
                if (await FileSystem.FileSystemProvider.FileExistsAsync(tempFile)) await FileSystem.FileSystemProvider.DeleteFileAsync(tempFile);
            }
        }
        public async Task RemovePagesAsync(List<PageDefinition> pages) {
            if (pages == null) return;
            foreach (PageDefinition page in pages)
                await RemovePageAsync(page.Url);
        }
        public async Task RemoveAllPagesAsync() {
            await InitSiteAsync();
            using (await _lockObject.LockAsync()) {
                Site.StaticPages = new Dictionary<string, StaticPages.StaticPageManager.PageEntry>();
                await RemoveAllPagesInternalAsync();
            }
        }
        private async Task RemoveAllPagesInternalAsync() {
            string folder = Path.Combine(Manager.SiteFolder, StaticFolder);
            if (await FileSystem.FileSystemProvider.DirectoryExistsAsync(folder)) {
                List<string> files = await FileSystem.FileSystemProvider.GetFilesAsync(folder, "*" + FileData.FileExtension);
                foreach (string file in files) {
                    try {
                        await FileSystem.FileSystemProvider.DeleteFileAsync(file);
                    } catch (Exception) { }
                }
            }
        }
    }
}
