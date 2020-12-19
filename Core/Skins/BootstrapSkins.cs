/* Copyright © 2021 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YetaWF.Core.Addons;
using YetaWF.Core.IO;
using YetaWF.Core.Support;

namespace YetaWF.Core.Skins {

    public partial class SkinAccess {

        private const string BootstrapThemeFile = "themelist.txt";

        public class BootstrapTheme {
            public string Name { get; set; } = null!;
            public string File { get; set; } = null!;
            public string? Description { get; set; }
        }

        public async Task<List<BootstrapTheme>> GetBootstrapThemeListAsync() {
            if (_BootstrapThemeList == null)
                _BootstrapThemeList = await LoadBootstrapThemesAsync();
            return _BootstrapThemeList;
        }
        private static List<BootstrapTheme>? _BootstrapThemeList;
        private static BootstrapTheme _BootstrapThemeDefault = null!;

        private async Task<List<BootstrapTheme>> LoadBootstrapThemesAsync() {
            string url = Manager.AddOnManager.GetAddOnNamedUrl(AreaRegistration.CurrentPackage.AreaName, "getbootstrap.com.bootswatch");
            string customUrl = VersionManager.GetCustomUrlFromUrl(url);
            string path = Utility.UrlToPhysical(url);
            string customPath = Utility.UrlToPhysical(customUrl);

            // use custom or default theme list
            string filename = Path.Combine(customPath, BootstrapThemeFile);
            if (!await FileSystem.FileSystemProvider.FileExistsAsync(filename))
                filename = Path.Combine(path, BootstrapThemeFile);

            List<string> lines = await FileSystem.FileSystemProvider.ReadAllLinesAsync(filename);
            List<BootstrapTheme> bsList = new List<BootstrapTheme>();

            foreach (string line in lines) {
                if (string.IsNullOrWhiteSpace(line)) continue;
                string[] s = line.Split(new char[] { ',' }, 3);
                string name = s[0].Trim();
                if (string.IsNullOrWhiteSpace(name)) throw new InternalError("Invalid/empty Bootstrap theme name");
                if (s.Length < 2)
                    throw new InternalError("Invalid Bootstrap theme entry: {0}", line);
                string file = s[1].Trim();
                string? description = null;
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
            return (from theme in bsList orderby theme.Name select theme).ToList();
        }

        internal async Task<string?> FindBootstrapSkinAsync(string? themeName) {
            string? folder = (from th in await GetBootstrapThemeListAsync() where th.Name == themeName select th.File).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(folder))
                return folder;
            return _BootstrapThemeDefault.File;
        }
        public static async Task<string> GetBootstrapDefaultSkinAsync() {
            SkinAccess skinAccess = new SkinAccess();
            await skinAccess.GetBootstrapThemeListAsync();
            return _BootstrapThemeDefault.Name;
        }
    }
}

