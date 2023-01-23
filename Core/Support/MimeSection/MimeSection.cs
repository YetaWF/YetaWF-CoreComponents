/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace YetaWF.Core.Support {

    public class MimeSection {

        public class MimeEntry {
            public string Type { get; set; } = null!;
            public string? Extensions { get; set; }
            public bool Download { get; set; }
            public dynamic? Dynamic { get; set; } // original entry (so we can access package-specific settings)

            public MimeEntry() {
                Download = true;
            }
        }

        // MIME Types

        public const string MimeSettingsFile = "MimeSettings.json";
        public const string ImageUse = "ImageUse";
        public const string PackageUse = "PackageUse";

        public Task InitAsync(string settingsFile) {
            if (!File.Exists(settingsFile)) // use local file system as we need this during initialization
                throw new InternalError("Mime settings not defined - file {0} not found", settingsFile);
            SettingsFile = settingsFile;
            dynamic settings = Utility.JsonDeserialize(File.ReadAllText(SettingsFile)); // use local file system as we need this during initialization

            dynamic mimeSection = settings["MimeSection"];
            List<MimeEntry> list = new List<MimeEntry> {
                // add required extensions (see FileExtensionContentTypeProvider.cs for complete list supported by .net core, but limited here)
                new MimeEntry { Extensions = ".js", Type = "application/javascript" },
                new MimeEntry { Extensions = ".css", Type = "text/css" },
                new MimeEntry { Extensions = ".gif", Type = "image/gif" },
                new MimeEntry { Extensions = ".png", Type = "image/png" },
                new MimeEntry { Extensions = ".jpe;.jpeg;.jpg", Type = "image/jpeg" },
                new MimeEntry { Extensions = ".webp;.webp-gen", Type = "image/webp" },
                new MimeEntry { Extensions = ".svg;.svgz", Type = "image/svg+xml" },
                new MimeEntry { Extensions = ".htm;.html", Type = "text/html" },
                new MimeEntry { Extensions = ".map", Type = "text/plain" },
                new MimeEntry { Extensions = ".xml", Type = "text/xml" }, // sitemap
                new MimeEntry { Extensions = ".txt", Type = "text/plain" }, // robots
                new MimeEntry { Extensions = ".ico", Type = "image/x-icon" } // favicon
            };

            // add specified extensions
            foreach (var t in mimeSection["MimeTypes"]) {
                string e = t.Extensions ?? "";
                list.Add(new MimeEntry { Extensions = e.ToLower(), Type = t.Type ?? "", Dynamic = t });
            }

            CachedEntries = list;
            return Task.CompletedTask;
        }

        private static string SettingsFile = null!;
        private static List<MimeEntry>? CachedEntries;

        public List<MimeEntry>? GetMimeTypes() {
            return CachedEntries;
        }
        public string? GetContentTypeFromExtension(string extension) {
            if (CachedEntries == null)
                return null;
            extension = extension.ToLower();
            foreach (MimeEntry entry in CachedEntries) {
                if (entry.Extensions != null) {
                    if (entry.Extensions.Contains(extension + ";") || entry.Extensions.EndsWith(extension))
                        return entry.Type;
                }
            }
            return null;
        }
        public bool CanUse(string? contentType, string resourceName) {
            if (CachedEntries == null)
                return false;
            contentType = contentType?.Trim().ToLower();
            if (string.IsNullOrWhiteSpace(contentType)) return false;
            foreach (MimeEntry entry in CachedEntries) {
                if (entry.Type == contentType) {
                    if (entry.Dynamic != null) {// built-in entries don't have dynamic section
                        try {
                            return (bool)entry.Dynamic[resourceName];
                        } catch (Exception) {
                            return false;
                        }
                    }
                }
            }
            return false;
        }
        //public static void Save() {
        //    string s = Utility.JsonSerialize(Settings);
        //    File.WriteAllText(SettingsFile, s);
        //}
    }
}
