/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using YetaWF.Core.Endpoints;
using YetaWF.Core.Packages;

namespace YetaWF.Core {

    /// <inheritdoc/>
    public class AreaRegistration : YetaWF.Core.Controllers.AreaRegistrationBase {
        /// <summary>
        /// Defines the current package, used by applications that need access to the YetaWF.Core.Packages.Package instance.
        /// </summary>
        public static Package CurrentPackage { get { return _CachedCurrentPackage ??= (_CachedCurrentPackage = Package.GetPackageFromAssembly(typeof(AreaRegistration).Assembly)); } }
        private static Package? _CachedCurrentPackage;
    }
}
