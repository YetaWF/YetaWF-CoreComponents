﻿/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.IO;
using YetaWF.Core.Extensions;
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

        private void InitSite() {
            lock (LockObject) {
                if (Sites == null) {
                    Sites = new Dictionary<int, SiteEntry>();
                    string folder = Path.Combine(Manager.SiteFolder, StaticFolder);
                    if (!YetaWFManager.Manager.Deployed) {
                        // For debug,development mode (i.e., not deployed) we'll always delete all saved static pages
                        // when restarting the site to avoid issues when switching between debug/release and javascript/css bundling
                        if (Directory.Exists(folder))
                            Directory.Delete(folder, true);
                    }
                    // when initializing, make sure the folder exists and create a don't deploy marker
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
                string[] files = Directory.GetFiles(folder, "http#*" + FileData.FileExtension);
                foreach (string file in files) { SaveFile(site, file, (entry, fileName) => { entry.FileName = fileName; }); }
                files = Directory.GetFiles(folder, "https#*" + FileData.FileExtension);
                foreach (string file in files) { SaveFile(site, file, (entry, fileName) => { entry.FileNameHttps = fileName; }); }
                files = Directory.GetFiles(folder, "http_popup#*" + FileData.FileExtension);
                foreach (string file in files) { SaveFile(site, file, (entry, fileName) => { entry.FileNamePopup = fileName; }); }
                files = Directory.GetFiles(folder, "https_popup#*" + FileData.FileExtension);
                foreach (string file in files) { SaveFile(site, file, (entry, fileName) => { entry.FileNamePopupHttps = fileName; }); }
            }
        }

        private void SaveFile(SiteEntry site, string file, Action<PageEntry, string> setFileName) {
            string localName = file.RemoveEndingAtIncluding('#');
            localName = Path.GetFileNameWithoutExtension(localName);
            string localUrl = FileData.ExtractNameFromFileName(localName);
            string localUrlLower = localUrl.ToLower();
            PageEntry entry;
            if (!site.StaticPages.TryGetValue(localUrlLower, out entry)) {
                entry = new StaticPages.StaticPageManager.PageEntry {
                    LocalUrl = localUrl,
                    StorageType = PageEntryEnum.Unknown,
                };
                site.StaticPages.Add(localUrlLower, entry);
            }
            setFileName(entry, file);
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

            string tempFile;
            if (Manager.CurrentRequest.Url.Scheme == "https") {
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
            lock (Site.StaticPages) {
                PageEntry entry;
                if (!Site.StaticPages.TryGetValue(localUrlLower, out entry)) {
                    entry = new StaticPages.StaticPageManager.PageEntry {
                        LocalUrl = localUrl,
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
            File.WriteAllText(tempFile, pageHtml);
        }

        private void SetFileName(PageEntry entry, string tempFile) {
            if (Manager.CurrentRequest.Url.Scheme == "https") {
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
            if (Manager.CurrentRequest.Url.Scheme == "https") {
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
        public string GetPage(string localUrl) {
            InitSite();
            string localUrlLower = localUrl.ToLower();
            PageEntry entry = null;
            if (Site.StaticPages.TryGetValue(localUrlLower, out entry)) {
                if (entry.StorageType == PageEntryEnum.Unknown) {
                    // Found an entry where a file exists but we don't know the storage type
                    // not technically thread safe, but ok even if two perform this at the same time, last one wins
                    PageDefinition page = PageDefinition.LoadPageDefinitionByUrl(localUrl);
                    if (page != null) {
                        entry.StorageType = PageEntryEnum.File;
                        if (page.StaticPage == PageDefinition.StaticPageEnum.YesMemory) {
                            try {
                                SetContents(entry, File.ReadAllText(entry.FileName));
                                entry.StorageType = PageEntryEnum.Memory;
                            } catch (System.Exception) { }
                        }
                    } else {
                        Site.StaticPages.Remove(localUrlLower);
                        return null;
                    }
                }
                if (entry.StorageType == PageEntryEnum.Memory) {
                    if (Manager.CurrentRequest.Url.Scheme == "https") {
                        if (Manager.IsInPopup) {
                            return entry.ContentPopupHttps;
                        } else {
                            return entry.ContentHttps;
                        }
                    } else {
                        if (Manager.IsInPopup) {
                            return entry.ContentPopup;
                        } else {
                            return entry.Content;
                        }
                    }
                } else /*if (entry.StorageType == PageEntryEnum.File)*/ {
                    string tempFile = null;
                    if (Manager.CurrentRequest.Url.Scheme == "https") {
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
            string localUrlLower = localUrl.ToLower();
            lock (Site.StaticPages) {
                Site.StaticPages.Remove(localUrlLower);
                string tempFile = Path.Combine(Manager.SiteFolder, StaticFolder, "http#" + FileData.MakeValidFileName(localUrl));
                if (File.Exists(tempFile)) File.Delete(tempFile);
                tempFile = Path.Combine(Manager.SiteFolder, StaticFolder, "https#" + FileData.MakeValidFileName(localUrl));
                if (File.Exists(tempFile)) File.Delete(tempFile);
                tempFile = Path.Combine(Manager.SiteFolder, StaticFolder, "http_popup#" + FileData.MakeValidFileName(localUrl));
                if (File.Exists(tempFile)) File.Delete(tempFile);
                tempFile = Path.Combine(Manager.SiteFolder, StaticFolder, "https_popup#" + FileData.MakeValidFileName(localUrl));
                if (File.Exists(tempFile)) File.Delete(tempFile);
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