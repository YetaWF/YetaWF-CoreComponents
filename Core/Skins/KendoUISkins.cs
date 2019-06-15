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

        private const string KendoThemeFile = "themelist.txt";

        public class KendoTheme {
            public string Name { get; set; }
            public string File { get; set; }
            public string Description { get; set; }
        }

        public async Task<List<KendoTheme>> GetKendoThemeListAsync() {
            if (_kendoThemeList == null)
                _kendoThemeList = await LoadKendoUIThemesAsync();
            return _kendoThemeList;
        }
        private static List<KendoTheme> _kendoThemeList;
        private static KendoTheme _kendoThemeDefault;

        private async Task<List<KendoTheme>> LoadKendoUIThemesAsync() {
            string kendoUIUrl = Manager.AddOnManager.GetAddOnNamedUrl(AreaRegistration.CurrentPackage.AreaName, "telerik.com.Kendo_UI_Core");
            string customUrl = VersionManager.GetCustomUrlFromUrl(kendoUIUrl);
            string path = Utility.UrlToPhysical(kendoUIUrl);
            string customPath = Utility.UrlToPhysical(customUrl);

            // use custom or default theme list
            string filename = Path.Combine(customPath, KendoThemeFile);
            if (!await FileSystem.FileSystemProvider.FileExistsAsync(filename))
                filename = Path.Combine(path, KendoThemeFile);

            List<string> lines = await FileSystem.FileSystemProvider.ReadAllLinesAsync(filename);
            List<KendoTheme> kendoList = new List<KendoTheme>();

            foreach (string line in lines) {
                if (string.IsNullOrWhiteSpace(line)) continue;
                string[] s = line.Split(new char[] { ',' }, 3);
                string name = s[0].Trim();
                if (string.IsNullOrWhiteSpace(name)) throw new InternalError("Invalid/empty Kendo theme name");
                if (s.Length < 2)
                    throw new InternalError("Invalid Kendo theme entry: {0}", line);
                string file = s[1].Trim();
                string description = null;
                if (s.Length > 2)
                    description = s[2].Trim();
                if (string.IsNullOrWhiteSpace(description))
                    description = null;
                kendoList.Add(new KendoTheme {
                    Name = name,
                    Description = description,
                    File = file,
                });
            }
            if (kendoList.Count == 0)
                throw new InternalError("No Kendo themes found");

            _kendoThemeDefault = kendoList[0];
            return (from theme in kendoList orderby theme.Name select theme).ToList();
        }

        public async Task<string> FindKendoUISkinAsync(string themeName) {
            string intName = (from th in await GetKendoThemeListAsync() where th.Name == themeName select th.File).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(intName))
                return intName;
            return _kendoThemeDefault.File;
        }
        public static async Task<string> GetKendoUIDefaultSkinAsync() {
            SkinAccess skinAccess = new SkinAccess();
            await skinAccess.GetKendoThemeListAsync();
            return _kendoThemeDefault.Name;
        }
    }
}
