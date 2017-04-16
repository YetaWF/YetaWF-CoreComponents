/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;
using System.IO;
using System.Linq;
using YetaWF.Core.Addons;
using YetaWF.Core.Support;

namespace YetaWF.Core.Skins {

    public partial class SkinAccess {

        private const string SyntaxHighlighterThemeFileMVC5 = "ThemelistMVC5.txt";
        private const string SyntaxHighlighterThemeFileMVC6 = "ThemelistMVC6.txt";

        public class SyntaxHighlighterTheme {
            public string Name { get; set; }
            public string File { get; set; }
            public string Description { get; set; }
        }

        public List<SyntaxHighlighterTheme> GetSyntaxHighlighterThemeList() {
            if (_syntaxHighlighterThemeList == null)
                LoadSyntaxHighlighterThemes();
            return _syntaxHighlighterThemeList;
        }
        private static List<SyntaxHighlighterTheme> _syntaxHighlighterThemeList;
        private static SyntaxHighlighterTheme _syntaxHighlighterThemeDefault;

        private List<SyntaxHighlighterTheme> LoadSyntaxHighlighterThemes() {
            string url = AddOnManager.GetAddOnGlobalUrl("alexgorbatchev.com", "SyntaxHighlighter", AddOnManager.UrlType.Base);
            string customUrl = VersionManager.GetCustomUrlFromUrl(url);
            string path = YetaWFManager.UrlToPhysical(url);
            string customPath = YetaWFManager.UrlToPhysical(customUrl);

            // use custom or default theme list
            string themeFile;
            if (YetaWFManager.AspNetMvc == YetaWFManager.AspNetMvcVersion.MVC5)
                themeFile = SyntaxHighlighterThemeFileMVC5;
            else
                themeFile = SyntaxHighlighterThemeFileMVC6;
            string filename = Path.Combine(customPath, themeFile);
            if (!File.Exists(filename))
                filename = Path.Combine(path, themeFile);

            string[] lines = File.ReadAllLines(filename);
            List<SyntaxHighlighterTheme> syntaxHighlighterList = new List<SyntaxHighlighterTheme>();

            foreach (string line in lines) {
                if (string.IsNullOrWhiteSpace(line)) continue;
                string[] s = line.Split(new char[] { ',' }, 3);
                string name = s[0].Trim();
                if (string.IsNullOrWhiteSpace(name)) throw new InternalError("Invalid/empty SyntaxHighlighter theme name");
                if (s.Length < 2)
                    throw new InternalError("Invalid SyntaxHighlighter theme entry: {0}", line);
                string file = s[1].Trim();
                if (file.StartsWith("\\")) {
                    string f = Path.Combine(YetaWFManager.RootFolder, file.Substring(1));
                    if (!File.Exists(f))
                        if (!File.Exists(f))
                            throw new InternalError("SyntaxHighlighter theme file not found: {0} - {1}", line, f);
                } else {
                    string f = Path.Combine(path, file);
                    if (!File.Exists(f))
                        if (!File.Exists(f))
                            throw new InternalError("SyntaxHighlighter theme file not found: {0} - {1}", line, f);
                }
                string description = null;
                if (s.Length > 2)
                    description = s[2].Trim();
                if (string.IsNullOrWhiteSpace(description))
                    description = null;
                syntaxHighlighterList.Add(new SyntaxHighlighterTheme {
                    Name = name,
                    Description = description,
                    File= file,
                });
            }
            if (syntaxHighlighterList.Count == 0)
                throw new InternalError("No SyntaxHighlighter themes found");

            _syntaxHighlighterThemeDefault = syntaxHighlighterList[0];
            _syntaxHighlighterThemeList = (from theme in syntaxHighlighterList orderby theme.Name select theme).ToList();
            return _syntaxHighlighterThemeList;
        }

        public string FindSyntaxHighlighterSkin(string themeName) {
            string intName = (from th in GetSyntaxHighlighterThemeList() where th.Name == themeName select th.File).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(intName))
                return intName;
            return _syntaxHighlighterThemeDefault.File;
        }
        public static string GetSyntaxHighlighterDefaultSkin() {
            SkinAccess skinAccess = new SkinAccess();
            skinAccess.GetSyntaxHighlighterThemeList();
            return _syntaxHighlighterThemeDefault.Name;
        }
    }
}
