/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.IO;

namespace YetaWF.Core.Support {

    public class MimeSection
#if MVC6
#else
        : IInitializeApplicationStartup 
#endif
        {

        // IInitializeApplicationStartup
        // IInitializeApplicationStartup
        // IInitializeApplicationStartup
#if MVC6
#else
        public void InitializeApplicationStartup() {
            string rootFolder = YetaWFManager.RootFolder;
            Init(Path.Combine(rootFolder, Globals.DataFolder, MimeSettingsFile));
        }
#endif

        // MIME Types

        public const string MimeSettingsFile = "MimeSettings.json";
        public const string ImageUse = "ImageUse";
        public const string FlashUse = "FlashUse";
        public const string PackageUse = "PackageUse";
#if MVC6
        public void Init(string settingsFile)
#else
        private static void Init(string settingsFile) 
#endif
        {
            if (!File.Exists(settingsFile))
                throw new InternalError("Mime settings not defined - file {0} not found", settingsFile);
            SettingsFile = settingsFile;
            Settings = YetaWFManager.JsonDeserialize(File.ReadAllText(SettingsFile));
        }

        private static string SettingsFile;
        private static dynamic Settings;

        public class MimeEntry {
            public string Extensions { get; set; }
            public string Type { get; set; }
        }

        public List<MimeEntry> GetMimeTypes() {
            dynamic mimeSection = Settings["MimeSection"];
            List<MimeEntry> list = new List<MimeEntry>();
            foreach (var t in mimeSection["MimeTypes"]) {
                list.Add(new MimeEntry { Extensions = t.Extensions, Type = t.Type });
            }
            return list;
        }
        public string GetContentTypeFromExtension(string extension) {
            dynamic mimeSection = Settings["MimeSection"];
            foreach (var t in mimeSection["MimeTypes"]) {
                string e = ((string)t["Extensions"]).Trim().ToLower();
                if (e.Contains(extension + ";") || e.EndsWith(extension))
                    return t.Type;
            }
            return null;
        }
        public bool CanUse(string contentType, string resourceName) {
            dynamic mimeSection = Settings["MimeSection"];
            contentType = contentType.Trim().ToLower();
            foreach (var entry in mimeSection["MimeTypes"]) {
                string type = ((string)entry["Type"]).Trim().ToLower();
                if (type == contentType) {
                    try {
                        return (bool)entry[resourceName];
                    } catch (Exception) {
                        return false;
                    }
                }
            }
            return false;
        }
        //public static void Save() {
        //    string s = YetaWFManager.JsonSerialize(Settings);
        //    File.WriteAllText(SettingsFile, s);
        //}
    }
}
