/* Copyright © 2021 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YetaWF.Core.IO;
using YetaWF.Core.Localize;
using YetaWF.Core.Log;
using YetaWF.Core.Support;

namespace YetaWF.Core.Skins {

    public partial class SkinAccess {

        private readonly List<string> RequiredPages = new List<string> { "Default", "Plain" };
        private readonly List<string> RequiredPopups = new List<string> { "Popup", "PopupSmall", "PopupMedium" };

        public async Task<SkinCollectionInfo> ParseSkinFileAsync(string domain, string product, string name, string folder) {

            SkinCollectionInfo? info = null;
            string fileName = string.Empty;
            if (info == null) {
                fileName = Path.Combine(folder, "Skin.yaml");
                if (await FileSystem.FileSystemProvider.FileExistsAsync(fileName))
                    info = await ParseSkinYamlFileAsync(fileName);
            }
            if (info == null) {
                fileName = Path.Combine(folder, "Skin.txt");
                if (await FileSystem.FileSystemProvider.FileExistsAsync(fileName))
                    info = await ParseSkinTextFileAsync(fileName);
            }
            if (info == null)
                throw new InternalError($"No skin definition found for {domain}/{product}/{name}");

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
            string text= await FileSystem.FileSystemProvider.ReadAllTextAsync(fileName);
            SkinCollectionInfo info = Utility.YamlDeserialize<SkinCollectionInfo>(text);
            return info;
        }

        private async Task<SkinCollectionInfo> ParseSkinTextFileAsync(string fileName) {

            Logging.AddLog("Parsing text {0}", fileName);

            List<string> lines = await FileSystem.FileSystemProvider.ReadAllLinesAsync(fileName);
            SkinCollectionInfo info = new SkinCollectionInfo();

            string line;
            int lineCount = 0;
            int totalLines = lines.Count;

            // get the collection description
            if (totalLines <= 0) throw new InternalError($"{fileName} Collection description missing - line {lineCount}");
            line = lines[lineCount++].Trim();
            info.Description = line;
            if (string.IsNullOrWhiteSpace(info.Description)) throw new InternalError($"{fileName} Collection description expected - line {lineCount}: {line}");

            // optional data, but must appear in the specified order
            info.UsingBootstrap = GetBool(fileName, lines, totalLines, ref lineCount, "UsingBootstrap", false);
            info.UseDefaultBootstrap = GetBool(fileName, lines, totalLines, ref lineCount, "UseDefaultBootstrap", false);
            info.UsingBootstrapButtons = GetBool(fileName, lines, totalLines, ref lineCount, "UsingBootstrapButtons", false);
            info.PartialFormCss = GetOptionalString(fileName, lines, totalLines, ref lineCount, "PartialFormCss", "");
            info.MinWidthForPopups = GetInt(fileName, lines, totalLines, ref lineCount, "MinWidthForPopups", 970);
            info.MinWidthForCondense = GetInt(fileName, lines, totalLines, ref lineCount, "MinWidthForCondense", info.MinWidthForPopups);

            // ::Pages::
            if (lineCount >= totalLines) throw new InternalError($"{fileName}({lineCount}): ::Pages:: section missing");
            line = lines[lineCount++].Trim();
            if (line != "::Pages::") throw new InternalError($"{fileName}({lineCount}): ::Pages:: section expected: {line}");
            lineCount = ExtractPages(fileName, lines, lineCount, totalLines, info);
            // ::Popups::
            if (lineCount >= totalLines) throw new InternalError($"{fileName}({lineCount}): ::Popups:: section missing");
            line = lines[lineCount++].Trim();
            if (line != "::Popups::") throw new InternalError($"{fileName}({lineCount}): ::Popups:: section expected: {line}");
            lineCount = ExtractPages(fileName, lines, lineCount, totalLines, info, Popups: true);
            //::Modules::
            if (lineCount >= totalLines) throw new InternalError($"{fileName}({lineCount}): ::Modules:: section missing");
            line = lines[lineCount++].Trim();
            if (line != "::Modules::") throw new InternalError($"{fileName}({lineCount}): ::Modules:: section expected: {line}");
            ExtractModules(fileName, lines, ref lineCount, totalLines, info);

            if (lineCount < totalLines)
                throw new InternalError($"{fileName}({lineCount}): Unexpected section encountered: {lines[lineCount]}");

            return info;
        }

        private int GetInt(string fileName, List<string> lines, int totalLines, ref int lineCount, string name, int dflt) {
            if (lineCount >= totalLines) throw new InternalError($"{fileName}({lineCount}): {name} missing");
            string line = lines[lineCount].Trim();
            string[] s = line.Split(new char[] { ' ' }, 2);
            if (s.Length != 2 || s[0] != name)
                return dflt;
            int val;
            try {
                val = Convert.ToInt32(s[1]);
            } catch (Exception) {
                throw new InternalError($"{fileName}({lineCount}): Invalid value specified for {name}");
            }
            lineCount++;
            return val;
        }

        private bool GetBool(string fileName, List<string> lines, int totalLines, ref int lineCount, string name, bool dflt) {
            if (lineCount >= totalLines) throw new InternalError($"{fileName}({lineCount}): {name} missing");
            string line = lines[lineCount].Trim();
            string[] s = line.Split(new char[] { ' ' }, 2);
            if (s.Length != 2 || s[0] != name)
                return dflt;
            lineCount++;
            return s[1] == "true" || s[1] == "1";
        }

        private string? GetOptionalString(string fileName, List<string> lines, int totalLines, ref int lineCount, string name, string dflt) {
            if (lineCount >= totalLines) throw new InternalError($"{fileName}({lineCount}): {name} missing");
            string line = lines[lineCount].Trim();
            string[] s = line.Split(new char[] { ' ' }, 2);
            if (s.Length < 1 || s[0] != name)
                return dflt;
            lineCount++;
            return s.Length > 1 ? s[1] : null;
        }

        private int ExtractPages(string fileName, List<string> lines, int lineCount, int totalLines, SkinCollectionInfo info, bool Popups = false) {
            while (lineCount < totalLines) {
                string line = lines[lineCount].Trim();
                if (line.StartsWith("::")) // start of a new section?
                    return lineCount;
                ++lineCount;

                // Get a page
                // Default;Default;Standard page (optional jumbotron, multi-column, footer);pageSkinDefault;8;16
                string[] s = line.Split(new string[] { ";" }, StringSplitOptions.None);
                int reqLength = Popups ? 9 : 6;
                if (s.Length != reqLength) throw new InternalError($"{fileName}({lineCount}): Invalid page skin entry: {line}");
                string name = s[0].Trim();
                if (name.Length == 0) throw new InternalError($"{fileName}({lineCount}): Invalid page name for page skin entry: {line}");
                string file = s[1].Trim();
                if (file.Length == 0) throw new InternalError($"{fileName}({lineCount}): Invalid page view name for page skin entry: {line}");
                string description = s[2].Trim();
                if (description.Length == 0) throw new InternalError($"{fileName}({lineCount}): Invalid description for page skin entry: {line}");
                string css = s[3].Trim();
                if (css.Length == 0) throw new InternalError($"{fileName}({lineCount}): Invalid Css for page skin entry: {line}");
                int width = 0, height = 0;
                bool maxButton = false;
                //int nextToken;
                if (Popups) {
                    try {
                        width = Convert.ToInt32(s[4]);
                        height = Convert.ToInt32(s[5]);
                        maxButton = Convert.ToInt32(s[6]) != 0;
                    } catch { }
                    description = this.__ResStr("descFmt", "{0} ({1} x {2} pixels)", description, width, height);
                    //nextToken = 7;
                } else {
                    //nextToken = 4;
                }

                PageSkinEntry entry = new PageSkinEntry() {
                    Name = name,
                    ViewName = file,
                    Description = description,
                    CSS = css,
                    Width = width,
                    Height = height,
                    MaximizeButton = maxButton,
                };
                if (Popups)
                    info.PopupSkins.Add(entry);
                else
                    info.PageSkins.Add(entry);
            }
            return lineCount;
        }
        private void ExtractModules(string fileName, List<string> lines, ref int lineCount, int totalLines, SkinCollectionInfo info) {
            while (lineCount < totalLines) {
                string line = lines[lineCount].Trim();
                if (line.StartsWith("::")) // start of a new section?
                    return;
                ++lineCount;

                // Get a module
                // Default,modStandard, Standard module
                string[] s = line.Split(new string[] { ";" }, StringSplitOptions.None);
                if (s.Length != 5) throw new InternalError($"{fileName}({lineCount}): Invalid module skin entry: {line}");
                string name = s[0].Trim();
                if (name.Length == 0) throw new InternalError($"{fileName}({lineCount}): Invalid module name for module skin entry: {line}");
                string css = s[1].Trim();
                if (css.Length == 0) throw new InternalError($"{fileName}({lineCount}): Invalid css for module skin entry: {line}");
                string description = s[2].Trim();
                if (description.Length == 0) throw new InternalError($"{fileName}({lineCount}): Invalid description for module skin entry: {line}");
                // width, height ignored - no longer used

                ModuleSkinEntry entry = new ModuleSkinEntry() {
                    Name = name,
                    CSS = css,
                    Description = description,
                };
                info.ModuleSkins.Add(entry);
            }
        }
    }
}
