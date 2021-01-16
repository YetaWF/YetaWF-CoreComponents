﻿/* Copyright © 2021 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.IO;
using YetaWF.Core.Addons;
using YetaWF.Core.Packages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Skins {

    public class SkinSVGs {

        protected static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        /// <summary>
        /// Returns a named SVG image from the current skin. If the current skin doesn't define the image, the YetaWF Core image is used instead.
        /// If neither exists, an empty string is returned.
        /// </summary>
        /// <param name="name">The SVG image name.</param>
        /// <returns>Returns the HTML representing the image (complete &lt;svg&gt; tag).</returns>
        // locate an icon image in the current skin
        public static string Get(Package package, string name) {

            string? html = null;
            // Search skin package
            VersionManager.AddOnProduct? addon = VersionManager.TryFindSkinVersion(Manager.CurrentSite.Skin.Collection);
            if (addon != null)
                html = addon.GetSVG($"{package.AreaName}_{name}");

            if (html == null) {
                addon = VersionManager.TryFindPackageVersion(package.AreaName);
                if (addon != null)
                    html = addon.GetSVG(name);
            }
#if DEBUG
            if (html == null)
                throw new InternalError($"SVG {name} in package {package.AreaName} not found");
#endif
            return html ?? string.Empty;
        }

        /// <summary>
        /// Returns a named SVG image from the current skin. If the current skin doesn't define the image, the YetaWF Core image is used instead.
        /// If neither exists, an empty string is returned.
        /// </summary>
        /// <param name="name">The SVG image name.</param>
        /// <returns>Returns the HTML representing the image (complete &lt;svg&gt; tag).</returns>
        // locate an icon image in the current skin
        public static string GetSkin(string name) {

            string? html = null;
            // Search skin package
            Package skinPackage = Manager.SkinInfo.Package;
            VersionManager.AddOnProduct? addon = VersionManager.TryFindSkinVersion(Manager.CurrentSite.Skin.Collection);
            if (addon != null)
                html = addon.GetSVG(name);
#if DEBUG
            if (html == null)
                throw new InternalError($"SVG {name} in current skin package not found");
#endif
            return html ?? string.Empty;
        }
    }
}
