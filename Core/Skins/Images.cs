/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.IO;
using YetaWF.Core.Addons;
using YetaWF.Core.Log;
using YetaWF.Core.Packages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Skins {

    public class SkinImages {

        // RESEARCH: This code could use some image lookup caching help

        protected static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        public const string SkinAddOnIconUrl_Format = "{0}Icons/{1}"; // skinaddon icon
        public const string GenericIcon = "Generic.png";

        public static Dictionary<string, string> PredefIcons = new Dictionary<string,string> {
           { "#Add", "Add.png" },
           { "#Browse", "Browse.png" },
           { "#Config", "Config.png" },
           { "#Display", "Display.png" },
           { "#Edit", "Edit.png" },
           { "#Generic", "Generic.png" },
           { "#Help", "Help.png" },
           { "#ModuleMenu", "ModuleMenu.png" },
           { "#Preview", "Preview.png" },
           { "#Remove", "Remove.png" },
           { "#WarningIcon", "WarningIcon.png" },
        };

        // locate an icon image in a package (for a package/module)
        public string FindIcon_Package(string imageUrl, Package package) {
            // Check package specific icons
            if (package == null)
                return FindIcon(imageUrl, null);
            else
                return FindIcon(imageUrl, VersionManager.FindModuleVersion(package.Domain, package.Product));
        }

        // locate an icon image for a template
        public string FindIcon_Template(string imageUrl, Package package, string template) {
            // Check package specific icons
            VersionManager.AddOnProduct version = VersionManager.FindTemplateVersion(package.Domain, package.Product, template);
            return FindIcon(imageUrl, version);
        }

        private string FindIcon(string imageUrl, VersionManager.AddOnProduct addOnVersion) {

            string url, urlCustom;
            string file;

            if (imageUrl.StartsWith("#"))
                PredefIcons.TryGetValue(imageUrl, out imageUrl);
            if (string.IsNullOrWhiteSpace(imageUrl))
                imageUrl = GenericIcon;

            if (addOnVersion != null) {
                // Check addon specific icons
                string addonUrl = addOnVersion.GetAddOnUrl();
                url = string.Format(SkinAddOnIconUrl_Format, addonUrl, imageUrl);
                urlCustom = VersionManager.GetCustomUrlFromUrl(url);
#if DEBUG
                Logging.AddLog("Searching {0}", urlCustom);
                Logging.AddLog("Searching {0}", url);
#endif
                file = YetaWFManager.UrlToPhysical(urlCustom);
                if (File.Exists(file))
                    return Manager.GetCDNUrl(urlCustom);
                file = YetaWFManager.UrlToPhysical(url);
                if (File.Exists(file))
                    return Manager.GetCDNUrl(url);
            }

            // get skin specific icon
            // TODO: Need a way for this to work in Ajax calls so we get the correct icons
            if (Manager.CurrentPage != null) {
                SkinDefinition skin = SkinDefinition.EvaluatedSkin(Manager.CurrentPage, Manager.IsInPopup);
                string skinCollection = skin.Collection;
                url = string.Format(SkinAddOnIconUrl_Format, VersionManager.GetAddOnSkinUrl(skinCollection), imageUrl);
                urlCustom = VersionManager.GetCustomUrlFromUrl(url);
#if DEBUG
                Logging.AddLog("Searching {0}", urlCustom);
                Logging.AddLog("Searching {0}", url);
#endif
                file = YetaWFManager.UrlToPhysical(urlCustom);
                if (File.Exists(file))
                    return Manager.GetCDNUrl(urlCustom);
                file = YetaWFManager.UrlToPhysical(url);
                if (File.Exists(file))
                    return Manager.GetCDNUrl(url);
            }

            // get fallback skin icon
            {
                string skinCollection = Manager.IsInPopup ? SkinDefinition.FallbackPopupSkin.Collection : SkinDefinition.FallbackSkin.Collection;
                url = string.Format(SkinAddOnIconUrl_Format, VersionManager.GetAddOnSkinUrl(skinCollection), imageUrl);
                urlCustom = VersionManager.GetCustomUrlFromUrl(url);
#if DEBUG
                Logging.AddLog("Searching {0}", urlCustom);
                Logging.AddLog("Searching {0}", url);
#endif
                file = YetaWFManager.UrlToPhysical(urlCustom);
                if (File.Exists(file))
                    return Manager.GetCDNUrl(urlCustom);
                file = YetaWFManager.UrlToPhysical(url);
                if (File.Exists(file))
                    return Manager.GetCDNUrl(url);

                // get generic icon
                url = string.Format(SkinAddOnIconUrl_Format, VersionManager.GetAddOnSkinUrl(skinCollection), GenericIcon);
                urlCustom = VersionManager.GetCustomUrlFromUrl(url);
#if DEBUG
                Logging.AddLog("Searching {0}", urlCustom);
                Logging.AddLog("Searching {0}", url);
#endif
                file = YetaWFManager.UrlToPhysical(urlCustom);
                if (File.Exists(file))
                    return Manager.GetCDNUrl(urlCustom);
                file = YetaWFManager.UrlToPhysical(url);
                if (File.Exists(file))
                    return Manager.GetCDNUrl(url);
            }
            throw new InternalError("Icon {0} not found", imageUrl);
        }
    }
}
