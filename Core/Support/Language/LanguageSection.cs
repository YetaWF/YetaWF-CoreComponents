/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using YetaWF.Core.Support;

namespace YetaWF.Core.Language {

    public class LanguageEntryElementCollection : List<LanguageEntryElement> { }
    public class LanguageEntryElement {
        public string Id { get; set; }
        public string ShortName { get; set; }
        public string Description { get; set; }
    }

    public static class LanguageSection {

        // Languages

        /// <summary>
        /// List of supported languages.
        /// </summary>
        public static LanguageEntryElementCollection Languages { get; private set; }

        /// <summary>
        /// Load the specified languages definition file.
        /// </summary>
        /// <param name="settingsFile">The languages definition file. May be null in which case only the default language US-en is available.</param>
        public static Task InitAsync(string settingsFile) {
            if (settingsFile == null || !File.Exists(settingsFile)) { // use local file system as we need this during initialization
                Languages = new LanguageEntryElementCollection {
                    new LanguageEntryElement {
                        Id = "en-US",
                        ShortName = "English",
                        Description = "US English"
                    },
                };
                return Task.CompletedTask;
            } else {
                dynamic settings = Utility.JsonDeserialize(File.ReadAllText(settingsFile)); // use local file system as we need this during initialization
                Languages = GetLanguages(settings);
                return Task.CompletedTask;
            }
        }

        private static string SettingsFile;
        private static dynamic Settings;

        private static LanguageEntryElementCollection GetLanguages(dynamic settings) {
            dynamic LanguageSection = settings["LanguageSection"];
            LanguageEntryElementCollection list = new LanguageEntryElementCollection();
            foreach (var t in LanguageSection["Languages"]) {
                list.Add(new LanguageEntryElement { Id = (string)t["Id"], ShortName = (string)t["ShortName"], Description = (string)t["Description"] });
            }
            return list;
        }
    }
}
