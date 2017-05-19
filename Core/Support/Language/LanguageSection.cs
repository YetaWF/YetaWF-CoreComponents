/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.IO;
using YetaWF.Core.Support;

namespace YetaWF.Core.Language {

    public class LanguageEntryElementCollection : List<LanguageEntryElement> { }
    public class LanguageEntryElement {
        public string Id { get; set; }
        public string ShortName { get; set; }
        public string Description { get; set; }
    }

    public class LanguageSection : IInitializeApplicationStartup {

        // IInitializeApplicationStartup
        // IInitializeApplicationStartup
        // IInitializeApplicationStartup

        public void InitializeApplicationStartup() {
            string rootFolder;
#if MVC6
            rootFolder = YetaWFManager.RootFolderWebProject;
#else
            rootFolder = YetaWFManager.RootFolder;
#endif
            Init(Path.Combine(rootFolder, Globals.DataFolder, LanguageSettingsFile));
        }

        // Languages

        public const string LanguageSettingsFile = "LanguageSettings.json";

        public static LanguageEntryElementCollection Languages { get; set; }

        private void Init(string settingsFile) {
            if (!File.Exists(settingsFile))
                throw new InternalError("Language settings not defined - file {0} not found", settingsFile);
            SettingsFile = settingsFile;
            Settings = YetaWFManager.JsonDeserialize(File.ReadAllText(SettingsFile));
            Languages = GetLanguages();
        }

        private static string SettingsFile;
        private static dynamic Settings;

        private LanguageEntryElementCollection GetLanguages() {
            dynamic LanguageSection = Settings["LanguageSection"];
            LanguageEntryElementCollection list = new LanguageEntryElementCollection();
            foreach (var t in LanguageSection["Languages"]) {
                list.Add(new LanguageEntryElement { Id = (string)t["Id"], ShortName = (string)t["ShortName"], Description = (string)t["Description"] });
            }
            return list;
        }
        //public static void Save() {
        //    string s = YetaWFManager.JsonSerialize(Settings);
        //    File.WriteAllText(SettingsFile, s);
        //}
    }
}
