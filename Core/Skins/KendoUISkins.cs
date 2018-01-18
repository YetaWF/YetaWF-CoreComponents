/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;
using System.IO;
using System.Linq;
using YetaWF.Core.Addons;
using YetaWF.Core.Support;

namespace YetaWF.Core.Skins {

    public partial class SkinAccess {

        private const string KendoThemeFile = "Themelist.txt";

        public class KendoTheme {
            public string Name { get; set; }
            public string File { get; set; }
            public string Description { get; set; }
        }

        public List<KendoTheme> GetKendoThemeList() {
            if (_kendoThemeList == null)
                LoadKendoUIThemes();
            return _kendoThemeList;
        }
        private static List<KendoTheme> _kendoThemeList;
        private static KendoTheme _kendoThemeDefault;

        private List<KendoTheme> LoadKendoUIThemes() {
            string url = VersionManager.KendoAddon.GetAddOnUrl();
            string customUrl = VersionManager.GetCustomUrlFromUrl(url);
            string path = YetaWFManager.UrlToPhysical(url);
            string customPath = YetaWFManager.UrlToPhysical(customUrl);

            // use custom or default theme list
            string filename = Path.Combine(customPath, KendoThemeFile);
            if (!File.Exists(filename))
                filename = Path.Combine(path, KendoThemeFile);

            string[] lines = File.ReadAllLines(filename);
            List<KendoTheme> kendoList = new List<KendoTheme>();

            foreach (string line in lines) {
                if (string.IsNullOrWhiteSpace(line)) continue;
                string[] s = line.Split(new char[] { ',' }, 3);
                string name = s[0].Trim();
                if (string.IsNullOrWhiteSpace(name)) throw new InternalError("Invalid/empty Kendo theme name");
                if (s.Length < 2)
                    throw new InternalError("Invalid Kendo theme entry: {0}", line);
                string file = s[1].Trim();
                if (file.StartsWith("\\")) {
                    string f = Path.Combine(YetaWFManager.RootFolder, file.Substring(1));
                    if (!File.Exists(f))
                        if (!File.Exists(f))
                            throw new InternalError("Kendo theme file not found: {0} - {1}", line, f);
                } else {
                    string f = Path.Combine(YetaWFManager.UrlToPhysical(VersionManager.KendoAddon.GetAddOnCssUrl()), file);
                    if (!File.Exists(f))
                        if (!File.Exists(f))
                            throw new InternalError("Kendo theme folder not found: {0} - {1}", line, f);
                }
                string description = null;
                if (s.Length > 2)
                    description = s[2].Trim();
                if (string.IsNullOrWhiteSpace(description))
                    description = null;
                kendoList.Add(new KendoTheme {
                    Name = name,
                    Description = description,
                    File= file,
                });
            }
            if (kendoList.Count == 0)
                throw new InternalError("No Kendo themes found");

            _kendoThemeDefault = kendoList[0];
            _kendoThemeList = (from theme in kendoList orderby theme.Name select theme).ToList();
            return _kendoThemeList;
        }

        internal string FindKendoUISkin(string themeName) {
            string intName = (from th in GetKendoThemeList() where th.Name == themeName select th.File).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(intName))
                return intName;
            return _kendoThemeDefault.File;
        }
        public static string GetKendoUIDefaultSkin() {
            SkinAccess skinAccess = new SkinAccess();
            skinAccess.GetKendoThemeList();
            return _kendoThemeDefault.Name;
        }
    }
}
