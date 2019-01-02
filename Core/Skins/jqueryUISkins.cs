/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YetaWF.Core.Addons;
using YetaWF.Core.Controllers;
using YetaWF.Core.IO;
using YetaWF.Core.Support;

namespace YetaWF.Core.Skins {

    public partial class SkinAccess {

        private const string ThemeFile = "Themelist.txt";

        public class JQueryTheme {
            public string Name { get; set; }
            public string File { get; set; }
            public string Description { get; set; }
        }

        public async Task<List<JQueryTheme>> GetJQueryThemeListAsync() {
            if (_jQueryThemeList == null)
                _jQueryThemeList = await LoadJQueryUIThemesAsync();
            return _jQueryThemeList;
        }
        private static List<JQueryTheme> _jQueryThemeList;
        private static JQueryTheme _jQueryThemeDefault;

        private async Task<List<JQueryTheme>> LoadJQueryUIThemesAsync() {
            string url = Manager.AddOnManager.GetAddOnNamedUrl(AreaRegistration.CurrentPackage.AreaName, "jqueryui.com.jqueryui-themes");
            string customUrl = VersionManager.GetCustomUrlFromUrl(url);
            string path = YetaWFManager.UrlToPhysical(url);
            string customPath = YetaWFManager.UrlToPhysical(customUrl);

            // use custom or default theme list
            string filename = Path.Combine(customPath, ThemeFile);
            if (!await FileSystem.FileSystemProvider.FileExistsAsync(filename))
                filename = Path.Combine(path, ThemeFile);

            List<string> lines = await FileSystem.FileSystemProvider.ReadAllLinesAsync(filename);
            List<JQueryTheme> jqList = new List<JQueryTheme>();

            foreach (string line in lines) {
                if (string.IsNullOrWhiteSpace(line)) continue;
                string[] s = line.Split(new char[] { ',' }, 3);
                string name = s[0].Trim();
                if (string.IsNullOrWhiteSpace(name)) throw new InternalError("Invalid/empty jQuery-ui theme name");
                if (s.Length < 2)
                    throw new InternalError("Invalid jQuery-ui theme entry: {0}", line);
                string file = s[1].Trim();
                string description = null;
                if (s.Length > 2)
                    description = s[2].Trim();
                if (string.IsNullOrWhiteSpace(description))
                    description = null;
                jqList.Add(new JQueryTheme {
                    Name = name,
                    Description = description,
                    File = file,
                });
            }
            if (jqList.Count == 0)
                throw new InternalError("No jQuery-UI themes found");

            _jQueryThemeDefault = jqList[0];
            return (from theme in jqList orderby theme.Name select theme).ToList();
        }

        public async Task<string> FindJQueryUISkinAsync(string themeName) {
            string folder = (from th in await GetJQueryThemeListAsync() where th.Name == themeName select th.File).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(folder))
                return folder;
            return _jQueryThemeDefault.File;
        }
        public static async Task<string> GetJQueryUIDefaultSkinAsync() {
            SkinAccess skinAccess = new SkinAccess();
            await skinAccess.GetJQueryThemeListAsync();
            return _jQueryThemeDefault.Name;
        }
    }
}
