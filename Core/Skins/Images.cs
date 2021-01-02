/* Copyright © 2021 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YetaWF.Core.Addons;
using YetaWF.Core.Components;
using YetaWF.Core.IO;
using YetaWF.Core.Packages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Skins {

    public class SkinImages {

        protected static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        public const string SkinAddOnIconUrl_Format = "{0}Icons/{1}"; // skinaddon icon
        public const string GenericIcon = "Generic.png";

        // Cache url icons searched (hits and misses)
        // Only Deployed builds use caching
        private static Dictionary<string, string?> Cache = new Dictionary<string, string?>();
        private enum CachedEnum {
            Unknown,
            NotFound,
            Yes,
        };

        // locate an icon image in a package (for a package/module)
        public async Task<string> FindIcon_PackageAsync(string imageUrl, Package package) {
            // Check package specific icons
            if (package == null)
                return await FindIconAsync(imageUrl, null);
            else
                return await FindIconAsync(imageUrl, VersionManager.FindPackageVersion(package.AreaName));
        }

        // locate an icon image for a template
        public async Task<string> FindIcon_TemplateAsync(string imageUrl, Package package, string template) {
            // Check package specific icons
            VersionManager.AddOnProduct version = VersionManager.FindTemplateVersion(package.AreaName, template);
            return await FindIconAsync(imageUrl, version);
        }

        private async Task<string> FindIconAsync(string imageUrl, VersionManager.AddOnProduct? addOnVersion) {

            string url, urlCustom;
            string file;

            if (imageUrl.StartsWith("#"))
                throw new InternalError($"Image urls starting with # must use {nameof(ImageHTML.BuildKnownIcon)}");
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
                        file = Utility.UrlToPhysical(urlCustom);
                        if (await FileSystem.FileSystemProvider.FileExistsAsync(file))
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
                        file = Utility.UrlToPhysical(url);
                        if (await FileSystem.FileSystemProvider.FileExistsAsync(file))
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
                SkinDefinition skin = Manager.CurrentSite.Skin;
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
                        file = Utility.UrlToPhysical(urlCustom);
                        if (await FileSystem.FileSystemProvider.FileExistsAsync(file))
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
                        file = Utility.UrlToPhysical(url);
                        if (await FileSystem.FileSystemProvider.FileExistsAsync(file))
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
                string skinCollection = SkinDefinition.FallbackSkin.Collection;
                url = string.Format(SkinAddOnIconUrl_Format, VersionManager.GetAddOnSkinUrl(skinCollection), imageUrl);
                urlCustom = VersionManager.GetCustomUrlFromUrl(url);
#if DEBUGME
                Logging.AddLog("Searching fallback icon {0} and {1}", url, urlCustom);
#endif
                switch (HasCache(urlCustom)) {
                    case CachedEnum.NotFound:
                        break;
                    case CachedEnum.Unknown:
                        file = Utility.UrlToPhysical(urlCustom);
                        if (await FileSystem.FileSystemProvider.FileExistsAsync(file))
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
                        file = Utility.UrlToPhysical(url);
                        if (await FileSystem.FileSystemProvider.FileExistsAsync(file))
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
                        file = Utility.UrlToPhysical(urlCustom);
                        if (await FileSystem.FileSystemProvider.FileExistsAsync(file))
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
                        file = Utility.UrlToPhysical(url);
                        if (await FileSystem.FileSystemProvider.FileExistsAsync(file))
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
            if (!Cache.TryGetValue(urlCustom, out string? cdnUrl) || cdnUrl == null)
                throw new InternalError($"{nameof(GetCache)} called for non-cached url");
            return cdnUrl;
        }
        private string AddCache(string urlCustom) {
            string cdnUrl = Manager.GetCDNUrl(urlCustom);
            if (YetaWFManager.Deployed) {
                try {// could fail if it was already added
#if DEBUG // minimize exception spam
                    if (!Cache.ContainsKey(urlCustom))
#endif
                        Cache.Add(urlCustom, cdnUrl);
                } catch (Exception) { }
            }
            return cdnUrl;
        }
        private void AddCacheNotFound(string url) {
            if (YetaWFManager.Deployed) {
                try {// could fail if it was already added
#if DEBUG // minimize exception spam
                    if (!Cache.ContainsKey(url))
#endif
                        Cache.Add(url, null);// marks url that no icon was found
                } catch (Exception) { }
            }
        }
        private CachedEnum HasCache(string urlCustom) {
            if (!Cache.TryGetValue(urlCustom, out string? cdnUrl))
                return CachedEnum.Unknown;
            if (string.IsNullOrWhiteSpace(cdnUrl))
                return CachedEnum.NotFound;
            return CachedEnum.Yes;
        }
    }
}
