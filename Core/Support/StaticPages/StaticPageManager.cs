/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.IO;
using YetaWF.Core.IO;
using YetaWF.Core.Pages;

namespace YetaWF.Core.Support.StaticPages {

    /// <summary>
    /// Keeps track of static pages.
    /// </summary>
    public class StaticPageManager {

        private const string StaticFolder = "YetaWF_StaticPages";

        private YetaWFManager Manager { get; set; }
        private SiteEntry Site { get; set; }
        private static object LockObject = new object();

        public enum PageEntryEnum {
            File = 0,
            Memory = 1,
            Best = 99, // Unknown storage type, update once a page is retrieved
        }
        public class SiteEntry {
            public int SiteIdentity { get; set; }
            public Dictionary<string, PageEntry> StaticPages { get; set; }
        }
        public class PageEntry {
            public PageEntryEnum StorageType { get; set; }
            public string Content { get; set; }
            public string FileName { get; set; }
        }
        public StaticPageManager(YetaWFManager manager) {
            Manager = manager;
        }

        public static Dictionary<int, SiteEntry> Sites { get; private set; }

        private void InitSite() {
            lock (LockObject) {
                if (Sites == null)
                    Sites = new Dictionary<int, SiteEntry>();
                SiteEntry site;
                if (!Sites.TryGetValue(Manager.CurrentSite.Identity, out site)) {
                    site = new SiteEntry { SiteIdentity = Manager.CurrentSite.Identity, StaticPages = new Dictionary<string, StaticPages.StaticPageManager.PageEntry>() };
                    Sites.Add(Manager.CurrentSite.Identity, site);
                    RetrieveStaticPages(site);
                }
                Site = site;
            }
        }

        private void RetrieveStaticPages(SiteEntry site) {
            string folder = Path.Combine(Manager.SiteFolder, StaticFolder);
            string[] files = Directory.GetFiles(folder, "*" + FileData.FileExtension);
            foreach (string file in files) {
                string localName = Path.GetFileNameWithoutExtension(file);
                string urlLocal = FileData.ExtractNameFromFileName(localName);
                site.StaticPages.Add(urlLocal, new StaticPages.StaticPageManager.PageEntry {
                    StorageType = PageEntryEnum.Best,
                    FileName = file,
                });
            }
        }

        public void AddPage(string localUrl, bool cache, string pageHtml) {
            InitSite();
            localUrl = localUrl.ToLower();
            string folder = Path.Combine(Manager.SiteFolder, StaticFolder);
            string tempFile = Path.Combine(folder, FileData.MakeValidFileName(localUrl));
            lock (Site.StaticPages) {
                Site.StaticPages.Remove(localUrl);
                if (cache) {
                    Site.StaticPages.Add(localUrl, new PageEntry {
                        Content = pageHtml,
                        FileName = tempFile,
                        StorageType = PageEntryEnum.Memory,
                    });
                } else {
                    Site.StaticPages.Add(localUrl, new PageEntry {
                        Content = null,
                        FileName = tempFile,
                        StorageType = PageEntryEnum.File,
                    });
                }
            }
            // save the file image
            Directory.CreateDirectory(folder);
            File.WriteAllText(tempFile, pageHtml);
        }
        public string GetPage(string localUrl) {
            InitSite();
            localUrl = localUrl.ToLower();
            PageEntry entry = null;
            if (Site.StaticPages.TryGetValue(localUrl, out entry)) {
                if (entry.StorageType == PageEntryEnum.Best) {
                    // Found an entry where a file exists but we don't know the storage type
                    // not technically thread safe, but ok even if two perform this at the same time, last one wins
                    PageDefinition page = PageDefinition.LoadPageDefinitionByUrl(localUrl);
                    if (page != null) {
                        entry.StorageType = PageEntryEnum.File;
                        if (page.StaticPage == PageDefinition.StaticPageEnum.YesMemory) {
                            try {
                                entry.Content = File.ReadAllText(entry.FileName);
                                entry.StorageType = PageEntryEnum.Memory;
                                return entry.Content;
                            } catch (System.Exception) { }
                        }
                    } else {
                        Site.StaticPages.Remove(localUrl);
                        return null;
                    }
                }
                if (entry.StorageType == PageEntryEnum.Memory) {
                    return entry.Content;
                } else /*if (entry.StorageType == PageEntryEnum.File)*/ {
                    string tempFile = Path.Combine(Manager.SiteFolder, StaticFolder, FileData.MakeValidFileName(localUrl));
                    try {
                        return File.ReadAllText(tempFile);
                    } catch (System.Exception) {
                        return null;
                    }
                }
            }
            return null;
        }
        public void RemovePage(string localUrl) {
            InitSite();
            localUrl = localUrl.ToLower();
            lock (Site.StaticPages) {
                Site.StaticPages.Remove(localUrl);
            }
        }
    }
}
