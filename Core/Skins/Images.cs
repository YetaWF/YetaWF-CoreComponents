/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.IO;
using YetaWF.Core.Addons;
using YetaWF.Core.Log;
using YetaWF.Core.Packages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Skins {

    public class SkinImages {

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
           { "#RemoveLight", "RemoveLight.png" },
           { "#Warning", "WarningIcon.png" },
        };

        // Cache url icons searched (hits and misses)
        // Only Deployed builds use caching
        private static Dictionary<string, string> Cache = new Dictionary<string, string>();
        private enum CachedEnum {
            Unknown,
            NotFound,
            Yes,
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
#if DEBUGME
                Logging.AddLog("Searching addon specific icon {0} and {1}", url, urlCustom);
#endif
                switch (HasCache(urlCustom)) {
                    case CachedEnum.NotFound:
                        break;
                    case CachedEnum.Unknown:
                        file = YetaWFManager.UrlToPhysical(urlCustom);
                        if (File.Exists(file))
                            return AddCache(urlCustom);
                        else
                            AddCacheNotFound(urlCustom);
                        break;
                    case CachedEnum.Yes:
                        return GetCache(urlCustom);
                }
                switch (HasCache(url)) {
                    case CachedEnum.NotFound:
                        break;
                    case CachedEnum.Unknown:
                        file = YetaWFManager.UrlToPhysical(url);
                        if (File.Exists(file))
                            return AddCache(url);
                        else
                            AddCacheNotFound(url);
                        break;
                    case CachedEnum.Yes:
                        return GetCache(url);
                }
            }

            // get skin specific icon
            // TODO: Need a way for this to work in Ajax calls so we get the correct icons
            if (Manager.CurrentPage != null) {
                SkinDefinition skin = SkinDefinition.EvaluatedSkin(Manager.CurrentPage, Manager.IsInPopup);
                string skinCollection = skin.Collection;
                url = string.Format(SkinAddOnIconUrl_Format, VersionManager.GetAddOnSkinUrl(skinCollection), imageUrl);
                urlCustom = VersionManager.GetCustomUrlFromUrl(url);
#if DEBUGME
                Logging.AddLog("Searching skin specific icon {0} and {1}", url, urlCustom);
#endif
                switch (HasCache(urlCustom)) {
                    case CachedEnum.NotFound:
                        break;
                    case CachedEnum.Unknown:
                        file = YetaWFManager.UrlToPhysical(urlCustom);
                        if (File.Exists(file))
                            return AddCache(urlCustom);
                        else
                            AddCacheNotFound(urlCustom);
                        break;
                    case CachedEnum.Yes:
                        return GetCache(urlCustom);
                }
                switch (HasCache(url)) {
                    case CachedEnum.NotFound:
                        break;
                    case CachedEnum.Unknown:
                        file = YetaWFManager.UrlToPhysical(url);
                        if (File.Exists(file))
                            return AddCache(url);
                        else
                            AddCacheNotFound(url);
                        break;
                    case CachedEnum.Yes:
                        return GetCache(url);
                }
            }

            // get fallback skin icon
            {
                string skinCollection = Manager.IsInPopup ? SkinDefinition.FallbackPopupSkin.Collection : SkinDefinition.FallbackSkin.Collection;
                url = string.Format(SkinAddOnIconUrl_Format, VersionManager.GetAddOnSkinUrl(skinCollection), imageUrl);
                urlCustom = VersionManager.GetCustomUrlFromUrl(url);
#if DEBUGME
                Logging.AddLog("Searching fallback icon {0} and {1}", url, urlCustom);
#endif
                switch (HasCache(urlCustom)) {
                    case CachedEnum.NotFound:
                        break;
                    case CachedEnum.Unknown:
                        file = YetaWFManager.UrlToPhysical(urlCustom);
                        if (File.Exists(file))
                            return AddCache(urlCustom);
                        else
                            AddCacheNotFound(urlCustom);
                        break;
                    case CachedEnum.Yes:
                        return GetCache(urlCustom);
                }
                switch (HasCache(url)) {
                    case CachedEnum.NotFound:
                        break;
                    case CachedEnum.Unknown:
                        file = YetaWFManager.UrlToPhysical(url);
                        if (File.Exists(file))
                            return AddCache(url);
                        else
                            AddCacheNotFound(url);
                        break;
                    case CachedEnum.Yes:
                        return GetCache(url);
                }

                // nothing found whatsoever, so get the generic icon
                url = string.Format(SkinAddOnIconUrl_Format, VersionManager.GetAddOnSkinUrl(skinCollection), GenericIcon);
                urlCustom = VersionManager.GetCustomUrlFromUrl(url);
#if DEBUGME
                Logging.AddLog("Searching generic icon {0} and {1}", url, urlCustom);
#endif
                switch (HasCache(urlCustom)) {
                    case CachedEnum.NotFound:
                        break;
                    case CachedEnum.Unknown:
                        file = YetaWFManager.UrlToPhysical(urlCustom);
                        if (File.Exists(file))
                            return AddCache(urlCustom);
                        else
                            AddCacheNotFound(urlCustom);
                        break;
                    case CachedEnum.Yes:
                        return GetCache(urlCustom);
                }
                switch (HasCache(url)) {
                    case CachedEnum.NotFound:
                        break;
                    case CachedEnum.Unknown:
                        file = YetaWFManager.UrlToPhysical(url);
                        if (File.Exists(file))
                            return AddCache(url);
                        else
                            AddCacheNotFound(url);
                        break;
                    case CachedEnum.Yes:
                        return GetCache(url);
                }
            }
            throw new InternalError("Icon {0} not found", imageUrl);
        }

        private string GetCache(string urlCustom) {
            string cdnUrl;
            if (!Cache.TryGetValue(urlCustom, out cdnUrl))
                return null;
            return cdnUrl;
        }
        private string AddCache(string urlCustom) {
            string cdnUrl = Manager.GetCDNUrl(urlCustom);
            if (Manager.Deployed) {
                try {// could fail if it was already added
                    Cache.Add(urlCustom, cdnUrl);
                } catch (Exception) { }
            }
            return cdnUrl;
        }
        private void AddCacheNotFound(string url) {
            if (Manager.Deployed) {
                try {// could fail if it was already added
                    Cache.Add(url, null);// marks url that no icon was found
                } catch (Exception) { }
            }
        }
        private CachedEnum HasCache(string urlCustom) {
            string cdnUrl;
            if (!Cache.TryGetValue(urlCustom, out cdnUrl))
                return CachedEnum.Unknown;
            if (string.IsNullOrWhiteSpace(cdnUrl))
                return CachedEnum.NotFound;
            return CachedEnum.Yes;
        }
    }
}
