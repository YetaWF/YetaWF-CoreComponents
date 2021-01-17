/* Copyright © 2021 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YetaWF.Core.IO;
using YetaWF.Core.Packages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Skins {

    public partial class SkinAccess {

        public const string SVGFolder = "SVG";

        private readonly List<string> RequiredPages = new List<string> { "Default", "Plain" };
        private readonly List<string> RequiredPopups = new List<string> { "Popup", "PopupSmall", "PopupMedium" };

        public async Task<SkinCollectionInfo> LoadSkinAsync(Package package, string domain, string product, string name, string folder) {

            return await ParseSkinFileAsync(package, domain, product, name, folder);
        }

        internal static async Task<Dictionary<string,string>> GetSVGsAsync(string path) {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            if (await FileSystem.FileSystemProvider.DirectoryExistsAsync(path)) {
                List<string> files = await FileSystem.FileSystemProvider.GetFilesAsync(path, "*.svg");
                foreach (string file in files) {
                    string html = await FileSystem.FileSystemProvider.ReadAllTextAsync(file);
                    string name = Path.GetFileNameWithoutExtension(file);
                    dict.Add(name, html);
                }
            }
            return dict;
        }

        private async Task<SkinCollectionInfo> ParseSkinFileAsync(Package package, string domain, string product, string name, string folder) {

            SkinCollectionInfo? info = null;
            string fileName = string.Empty;
            if (info == null) {
                fileName = Path.Combine(folder, "Skin.yaml");
                if (await FileSystem.FileSystemProvider.FileExistsAsync(fileName))
                    info = await ParseSkinYamlFileAsync(fileName);
            }
            if (info == null)
                throw new InternalError($"No skin definition found for {domain}/{product}/{name}");

            info.Package = package;
            info.Name = $"{domain}/{product}/{name}";
            info.Folder = folder;
            info.AreaName = $"{domain}_{product}";

            if (info.PageSkins.Count < 1) throw new InternalError($"{fileName}: Skin collection {info.Name} has no page skins");
            if (info.PopupSkins.Count < 1) throw new InternalError($"{fileName}: Skin collection {info.Name} has no popup skins");
            if (info.ModuleSkins.Count < 1) throw new InternalError($"{fileName}: Skin collection {info.Name} has no module skins");

            foreach (string p in RequiredPages) {
                if ((from s in info.PageSkins where s.ViewName == p select s).FirstOrDefault() == null)
                    throw new InternalError($"{fileName} Skin collection {info.Name} has no {p} page");
            }
            foreach (string p in RequiredPopups) {
                if ((from s in info.PopupSkins where s.ViewName == p select s).FirstOrDefault() == null)
                    throw new InternalError($"{fileName} Skin collection {info.Name} has no {p} popup");
            }
            return info;
        }

        private async Task<SkinCollectionInfo> ParseSkinYamlFileAsync(string fileName) {
            string text = await FileSystem.FileSystemProvider.ReadAllTextAsync(fileName);
            SkinCollectionInfo info = Utility.YamlDeserialize<SkinCollectionInfo>(text);
            return info;
        }
    }
}
