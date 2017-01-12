/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.IO;
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
        private static object LockObject = new object();

        public enum PageEntryEnum {
            [EnumDescription("File", "Static pages cached using a local file")]
            File = 0,
            [EnumDescription("Memory", "Static pages cached in memory")]
            Memory = 1,
            [EnumDescription("Undetermined", "Cached static page found in a local file, but may be cached in memory once loaded")]
            Unknown = 99, // Unknown storage type, update once a page is retrieved
        }
        public class SiteEntry {
            public int SiteIdentity { get; set; }
            public Dictionary<string, PageEntry> StaticPages { get; set; }
        }
        public class PageEntry {
            public string LocalUrl { get; set; }
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
                if (Sites == null) {
                    Sites = new Dictionary<int, SiteEntry>();
                    // when initializing, make sure the folder exists and create a don't deploy marker
                    string folder = Path.Combine(Manager.SiteFolder, StaticFolder);
                    Directory.CreateDirectory(folder);
                    File.WriteAllText(Path.Combine(folder, "dontdeploy.txt"), "");
                }
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
            if (Directory.Exists(folder)) {
                string[] files = Directory.GetFiles(folder, "*" + FileData.FileExtension);
                foreach (string file in files) {
                    string localName = Path.GetFileNameWithoutExtension(file);
                    string localUrl = FileData.ExtractNameFromFileName(localName);
                    site.StaticPages.Add(localUrl, new StaticPages.StaticPageManager.PageEntry {
                        LocalUrl = localUrl,
                        StorageType = PageEntryEnum.Unknown,
                        FileName = file,
                    });
                }
            }
        }

        public List<PageEntry> GetSiteStaticPages() {
            InitSite();
            List<PageEntry> list = new List<PageEntry>(Site.StaticPages.Values);
            return list;
        }

        public void AddPage(string localUrl, bool cache, string pageHtml) {
            InitSite();
            string localUrlLower = localUrl.ToLower();
            string folder = Path.Combine(Manager.SiteFolder, StaticFolder);
            string tempFile = Path.Combine(folder, FileData.MakeValidFileName(localUrl));
            lock (Site.StaticPages) {
                Site.StaticPages.Remove(localUrlLower);
                if (cache) {
                    Site.StaticPages.Add(localUrlLower, new PageEntry {
                        LocalUrl = localUrl,
                        Content = pageHtml,
                        FileName = tempFile,
                        StorageType = PageEntryEnum.Memory,
                    });
                } else {
                    Site.StaticPages.Add(localUrlLower, new PageEntry {
                        LocalUrl = localUrl,
                        Content = null,
                        FileName = tempFile,
                        StorageType = PageEntryEnum.File,
                    });
                }
            }
            // save the file image
            File.WriteAllText(tempFile, pageHtml);
        }
        public string GetPage(string localUrl) {
            InitSite();
            localUrl = localUrl.ToLower();
            PageEntry entry = null;
            if (Site.StaticPages.TryGetValue(localUrl, out entry)) {
                if (entry.StorageType == PageEntryEnum.Unknown) {
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
                string tempFile = Path.Combine(Manager.SiteFolder, StaticFolder, FileData.MakeValidFileName(localUrl));
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }
        public void RemoveAllPages() {
            InitSite();
            lock (Site.StaticPages) {
                Site.StaticPages = new Dictionary<string, StaticPages.StaticPageManager.PageEntry>();
                string folder = Path.Combine(Manager.SiteFolder, StaticFolder);
                if (Directory.Exists(folder)) {
                    string[] files = Directory.GetFiles(folder, "*" + FileData.FileExtension);
                    foreach (string file in files) {
                        try {
                            File.Delete(file);
                        } catch (Exception) { }
                    }
                }
            }
        }
    }
}
