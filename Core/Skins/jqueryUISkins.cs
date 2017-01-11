/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;
using System.IO;
using System.Linq;
using YetaWF.Core.Addons;
using YetaWF.Core.Support;

namespace YetaWF.Core.Skins {

    public partial class SkinAccess {

        private const string ThemeFile = "Themelist.txt";

        public class JQueryTheme {
            public string Name { get; set; }
            public string File { get; set; }
            public string Description { get; set; }
        }

        public List<JQueryTheme> GetJQueryThemeList() {
            if (_jQueryThemeList == null)
                LoadJQueryUIThemes();
            return _jQueryThemeList;
        }
        private static List<JQueryTheme> _jQueryThemeList;
        private static JQueryTheme _jQueryThemeDefault;

        private List<JQueryTheme> LoadJQueryUIThemes() {
            string url = AddOnManager.GetAddOnGlobalUrl("jqueryui.com", "jqueryui-themes", AddOnManager.UrlType.Base);
            string customUrl = VersionManager.GetCustomUrlFromUrl(url);
            string path = YetaWFManager.UrlToPhysical(url);
            string customPath = YetaWFManager.UrlToPhysical(customUrl);

            // use custom or default theme list
            string filename = Path.Combine(customPath, ThemeFile);
            if (!File.Exists(filename))
                filename = Path.Combine(path, ThemeFile);

            string[] lines = File.ReadAllLines(filename);
            List<JQueryTheme> jqList = new List<JQueryTheme>();

            foreach (string line in lines) {
                if (string.IsNullOrWhiteSpace(line)) continue;
                string[] s = line.Split(new char[] { ',' }, 3);
                string name = s[0].Trim();
                if (string.IsNullOrWhiteSpace(name)) throw new InternalError("Invalid/empty jQuery-ui theme name");
                if (s.Length < 2)
                    throw new InternalError("Invalid jQuery-ui theme entry: {0}", line);
                string file = s[1].Trim();
                if (file.StartsWith("\\")) {
                    string f = Path.Combine(YetaWFManager.RootFolder, file.Substring(1));
                    if (!File.Exists(f))
                        if (!File.Exists(f))
                            throw new InternalError("jQuery-ui theme file not found: {0} - {1}", line, f);
                } else {
                    string f = Path.Combine(path, file);
                    if (!File.Exists(f))
                        if (!File.Exists(f))
                            throw new InternalError("jQuery-ui theme file not found: {0} - {1}", line, f);
                }
                string description = null;
                if (s.Length > 2)
                    description = s[2].Trim();
                if (string.IsNullOrWhiteSpace(description))
                    description = null;
                jqList.Add(new JQueryTheme {
                    Name = name,
                    Description = description,
                    File= file,
                });
            }
            if (jqList.Count == 0)
                throw new InternalError("No jQuery-UI themes found");

            _jQueryThemeDefault = jqList[0];
            _jQueryThemeList = (from theme in jqList orderby theme.Name select theme).ToList();
            return _jQueryThemeList;
        }

        internal string FindJQueryUISkin(string themeName) {
            string folder = (from th in GetJQueryThemeList() where th.Name == themeName select th.File).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(folder))
                return folder;
            return _jQueryThemeDefault.File;
        }
        public static string GetJQueryUIDefaultSkin() {
            SkinAccess skinAccess = new SkinAccess();
            skinAccess.GetJQueryThemeList();
            return _jQueryThemeDefault.Name;
        }
    }
}
