/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.IO;
using System.Linq;
using YetaWF.Core.Localize;
using YetaWF.Core.Log;
using YetaWF.Core.Support;

namespace YetaWF.Core.Skins {

    public partial class SkinAccess {

        public SkinCollectionInfo ParseSkinFile(string domain, string product, string name, string folder) {

            string fileName = Path.Combine(folder, "Skin.txt");

            Logging.AddLog("Parsing {0}", fileName);

            string[] lines = File.ReadAllLines(fileName);
            SkinCollectionInfo info = new SkinCollectionInfo() {
                CollectionName = domain + "/" + product + "/" + name,
                Folder = folder
            };
            string line;
            int lineCount = 0;
            int totalLines = lines.Length;

            // get the collection description
            if (totalLines <= 0) throw new InternalError("Collection description missing - line {0}", lineCount);
            line = lines[lineCount++].Trim();
            info.CollectionDescription = line;
            if (string.IsNullOrWhiteSpace(info.CollectionDescription)) throw new InternalError("Collection description expected - line {0}: {1}", lineCount, line);

            info.JQuerySkin = GetOptionalString(fileName, lines, totalLines, ref lineCount, "JQuerySkin");
            info.KendoSkin = GetOptionalString(fileName, lines, totalLines, ref lineCount, "KendoSkin");
            info.UsingBootstrap = GetBool(fileName, lines, totalLines, ref lineCount, "UsingBootstrap");
            info.UseDefaultBootstrap = GetBool(fileName, lines, totalLines, ref lineCount, "UseDefaultBootstrap");
            info.UsingBootstrapButtons = GetBool(fileName, lines, totalLines, ref lineCount, "UsingBootstrapButtons");
            info.MinWidthForPopups = GetInt(fileName, lines, totalLines, ref lineCount, "MinWidthForPopups");

            // ::Pages::
            if (lineCount >= totalLines) throw new InternalError("::Pages:: section missing - line {0}", lineCount);
            line = lines[lineCount++].Trim();
            if (line != "::Pages::") throw new InternalError("::Pages:: section expected - line {0}: {1}", lineCount, line);
            ExtractPages(fileName, lines, ref lineCount, totalLines, info);
            // ::Popups::
            if (lineCount >= totalLines) throw new InternalError("::Popups:: section missing - line {0}", lineCount);
            line = lines[lineCount++].Trim();
            if (line != "::Popups::") throw new InternalError("::Popups:: section expected - line {0}: {1}", lineCount, line);
            ExtractPages(fileName, lines, ref lineCount, totalLines, info, Popups: true);
            //::Modules::
            if (lineCount >= totalLines) throw new InternalError("::Modules:: section missing - line {0}", lineCount);
            line = lines[lineCount++].Trim();
            if (line != "::Modules::") throw new InternalError("::Modules:: section expected - line {0}: {1}", lineCount, line);
            ExtractModules(lines, ref lineCount, totalLines, info);

            if (lineCount < totalLines)
                throw new InternalError("Unexpected section encountered - line {0}: {1}", lineCount, lines[lineCount]);

            if (info.PageSkins.Count < 1) throw new InternalError("Skin collection {0} has no page skins", info.CollectionName);
            if (info.PopupSkins.Count < 1) throw new InternalError("Skin collection {0} has no popup skins", info.CollectionName);
            if (info.ModuleSkins.Count < 1) throw new InternalError("Skin collection {0} has no module skins", info.CollectionName);

            if ((from s in info.PageSkins where s.FileName == SkinAccess.FallbackPageFileName select s).FirstOrDefault() == null)
                throw new InternalError("Skin collection {0} has no {1}", info.CollectionName, SkinAccess.FallbackPageFileName);
            if ((from s in info.PageSkins where s.FileName == SkinAccess.FallbackPagePlainFileName select s).FirstOrDefault() == null)
                throw new InternalError("Skin collection {0} has no {1}", info.CollectionName, SkinAccess.FallbackPagePlainFileName);

            if ((from s in info.PopupSkins where s.FileName == SkinAccess.FallbackPopupFileName select s).FirstOrDefault() == null)
                throw new InternalError("Skin collection {0} has no {1}", info.CollectionName, SkinAccess.FallbackPopupFileName);
            if ((from s in info.PopupSkins where s.FileName == SkinAccess.FallbackPopupMediumFileName select s).FirstOrDefault() == null)
                throw new InternalError("Skin collection {0} has no {1}", info.CollectionName, SkinAccess.FallbackPopupMediumFileName);
            if ((from s in info.PopupSkins where s.FileName == SkinAccess.FallbackPopupSmallFileName select s).FirstOrDefault() == null)
                throw new InternalError("Skin collection {0} has no {1}", info.CollectionName, SkinAccess.FallbackPopupSmallFileName);

            return info;
        }

        private int GetInt(string fileName, string[] lines, int totalLines, ref int lineCount, string name) {
            if (lineCount >= totalLines) throw new InternalError("{0} missing - line {1} ({2})", name, lineCount, fileName);
            string line = lines[lineCount++].Trim();
            string[] s = line.Split(new char[] { ' ' }, 2);
            if (s.Length != 2 || s[0] != name) throw new InternalError("{0} expected with true/false - line {1} ({2})", name, lineCount, fileName);
            int val;
            try {
                val = Convert.ToInt32(s[1]);
            } catch (Exception) {
                throw new InternalError("Invalid value specified for {0} - line {1} ({2})", name, lineCount, fileName);
            }
            return val;
        }

        private bool GetBool(string fileName, string[] lines, int totalLines, ref int lineCount, string name) {
            if (lineCount >= totalLines) throw new InternalError("{0} missing - line {1} ({2})", name, lineCount, fileName);
            string line = lines[lineCount++].Trim();
            string[] s = line.Split(new char[] { ' ' }, 2);
            if (s.Length != 2 || s[0] != name) throw new InternalError("{0} expected with true/false - line {1} ({2})", name, lineCount, fileName);
            return s[1] == "true" || s[1] == "1";
        }

        private string GetOptionalString(string fileName, string[] lines, int totalLines, ref int lineCount, string name) {
            if (lineCount >= totalLines) throw new InternalError("{0} missing - line {1} ({2})", name, lineCount, fileName);
            string line = lines[lineCount++].Trim();
            string[] s = line.Split(new char[] { ' ' }, 2);
            if (s.Length < 1 || s[0] != name) throw new InternalError("{0} expected - line {1} ({2})", name, lineCount, fileName);
            return s.Length > 1 ? s[1] : null;
        }

        private void ExtractPages(string fileName, string[] lines, ref int lineCount, int totalLines, SkinCollectionInfo info, bool Popups = false)
        {
            while (lineCount < totalLines) {
                string line = lines[lineCount].Trim();
                if (line.StartsWith("::")) // start of a new section?
                    return;
                ++lineCount;

                // Get a page
                // Default,Default.cshtml, Standard page with a header, footer and a main pane
                string[] s = line.Split(new string[] { ";" }, StringSplitOptions.None);
                int reqLength = Popups ? 9 : 6;
                if (s.Length != reqLength) throw new InternalError("Invalid page skin entry - line {0}: {1} ({2})", lineCount, line, fileName);
                string name = s[0].Trim();
                if (name.Length == 0) throw new InternalError("Invalid page name for page skin entry - line {0}: {1} ({2})", lineCount, line, fileName);
                string file = s[1].Trim();
                if (file.Length == 0) throw new InternalError("Invalid file name for page skin entry - line {0}: {1} ({2})", lineCount, line, fileName);
                if (!File.Exists(Path.Combine(info.Folder, Popups?PopupFolder:PageFolder, file))) throw new InternalError("File for skin entry not found - line {0}: {1} ({2})", lineCount, line, fileName);
                string description = s[2].Trim();
                if (description.Length == 0) throw new InternalError("Invalid description for page skin entry - line {0}: {1} ({2})", lineCount, line, fileName);
                string css = s[3].Trim();
                if (css.Length == 0) throw new InternalError("Invalid Css for page skin entry - line {0}: {1} ({2})", lineCount, line, fileName);
                int width = 0, height = 0;
                bool maxButton = false;
                int nextToken;
                if (Popups) {
                    try {
                        width = Convert.ToInt32(s[4]);
                        height = Convert.ToInt32(s[5]);
                        maxButton = Convert.ToInt32(s[6]) != 0;
                    } catch { }
                    description = this.__ResStr("descFmt", "{0} ({1} x {2} pixels)", description, width, height);
                    nextToken = 7;
                } else {
                    nextToken = 4;
                }
                int charWidth = 0, charHeight = 0;
                try {
                    charWidth = Convert.ToInt32(s[nextToken]);
                    charHeight = Convert.ToInt32(s[nextToken+1]);
                } catch { }
                if (charWidth <= 0 || charHeight <= 0) throw new InternalError("Invalid character width/height for page skin entry - line {0}: {1} ({2})", lineCount, line, fileName);

                PageSkinEntry entry = new PageSkinEntry() {
                    Name = name,
                    FileName = file,
                    Description = description,
                    Css = css,
                    Width = width,
                    Height = height,
                    MaximizeButton = maxButton,
                    CharWidthAvg = charWidth,
                    CharHeight = charHeight,
                };
                if (Popups)
                    info.PopupSkins.Add(entry);
                else
                    info.PageSkins.Add(entry);
            }
        }
        private void ExtractModules(string[] lines, ref int lineCount, int totalLines, SkinCollectionInfo info) {
            while (lineCount < totalLines) {
                string line = lines[lineCount].Trim();
                if (line.StartsWith("::")) // start of a new section?
                    return;
                ++lineCount;

                // Get a module
                // Default,modStandard, Standard module
                string[] s = line.Split(new string[] { ";" }, StringSplitOptions.None);
                if (s.Length != 5) throw new InternalError("Invalid module skin entry - line {0}: {1}", lineCount, line);
                string name = s[0].Trim();
                if (name.Length == 0) throw new InternalError("Invalid module name for module skin entry - line {0}: {1}", lineCount, line);
                string css = s[1].Trim();
                if (css.Length == 0) throw new InternalError("Invalid css for module skin entry - line {0}: {1}", lineCount, line);
                string description = s[2].Trim();
                if (description.Length == 0) throw new InternalError("Invalid description for module skin entry - line {0}: {1}", lineCount, line);
                int width = 0, height = 0;
                try {
                    width = Convert.ToInt32(s[3]);
                    height = Convert.ToInt32(s[4]);
                } catch { }
                if (width <= 0 || height <= 0) throw new InternalError("Invalid width/height for module skin entry - line {0}: {1}", lineCount, line);

                ModuleSkinEntry entry = new ModuleSkinEntry() {
                    Name = name,
                    CssClass = css,
                    Description = description,
                    CharWidthAvg = width,
                    CharHeight = height,
                };
                info.ModuleSkins.Add(entry);
            }
        }
    }
}
