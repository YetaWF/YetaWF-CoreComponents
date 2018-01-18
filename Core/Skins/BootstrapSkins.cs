/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;
using System.IO;
using System.Linq;
using YetaWF.Core.Addons;
using YetaWF.Core.Support;

namespace YetaWF.Core.Skins {

    public partial class SkinAccess {

        private const string BootstrapThemeFile = "Themelist.txt";

        public class BootstrapTheme {
            public string Name { get; set; }
            public string File { get; set; }
            public string Description { get; set; }
        }

        public List<BootstrapTheme> GetBootstrapThemeList() {
            if (_BootstrapThemeList == null)
                LoadBootstrapThemes();
            return _BootstrapThemeList;
        }
        private static List<BootstrapTheme> _BootstrapThemeList;
        private static BootstrapTheme _BootstrapThemeDefault;

        private List<BootstrapTheme> LoadBootstrapThemes() {
            string url = AddOnManager.GetAddOnGlobalUrl("getbootstrap.com", "bootswatch", AddOnManager.UrlType.Base);
            string customUrl = VersionManager.GetCustomUrlFromUrl(url);
            string path = YetaWFManager.UrlToPhysical(url);
            string customPath = YetaWFManager.UrlToPhysical(customUrl);

            // use custom or default theme list
            string filename = Path.Combine(customPath, BootstrapThemeFile);
            if (!File.Exists(filename))
                filename = Path.Combine(path, BootstrapThemeFile);

            string[] lines = File.ReadAllLines(filename);
            List<BootstrapTheme> bsList = new List<BootstrapTheme>();

            foreach (string line in lines) {
                if (string.IsNullOrWhiteSpace(line)) continue;
                string[] s = line.Split(new char[] { ',' }, 3);
                string name = s[0].Trim();
                if (string.IsNullOrWhiteSpace(name)) throw new InternalError("Invalid/empty Bootstrap theme name");
                if (s.Length < 2)
                    throw new InternalError("Invalid Bootstrap theme entry: {0}", line);
                string file = s[1].Trim();
                string description = null;
                if (s.Length > 2)
                    description = s[2].Trim();
                if (string.IsNullOrWhiteSpace(description))
                    description = null;
                bsList.Add(new BootstrapTheme {
                    Name = name,
                    Description = description,
                    File = file,
                });
            }
            if (bsList.Count == 0)
                throw new InternalError("No Bootstrap themes found");

            _BootstrapThemeDefault = bsList[0];
            _BootstrapThemeList = (from theme in bsList orderby theme.Name select theme).ToList();
            return _BootstrapThemeList;
        }

        internal string FindBootstrapSkin(string themeName) {
            string folder = (from th in GetBootstrapThemeList() where th.Name == themeName select th.File).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(folder))
                return folder;
            return _BootstrapThemeDefault.File;
        }
        public static string GetBootstrapDefaultSkin() {
            SkinAccess skinAccess = new SkinAccess();
            skinAccess.GetBootstrapThemeList();
            return _BootstrapThemeDefault.Name;
        }
    }
}

