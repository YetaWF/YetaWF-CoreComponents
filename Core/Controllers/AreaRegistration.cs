/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

﻿using System.Web.Mvc;
using YetaWF.Core.Log;
using YetaWF.Core.Packages;

namespace YetaWF.Core.Controllers {

    // RESEARCH: http://stephenwalther.com/archive/2015/02/07/asp-net-5-deep-dive-routing

    public partial class AreaRegistration : System.Web.Mvc.AreaRegistration {

        public AreaRegistration(out Package currentPackage) {
            // get area name from package information
            currentPackage = Package.GetPackageFromAssembly(GetType().Assembly);
            _area = currentPackage.AreaName;
        }
        public override string AreaName {
            get { return _area; }
        }
        private string _area;

        public override void RegisterArea(AreaRegistrationContext context) {

            Logging.AddLog("Found {0} in namespace {1}", AreaName, GetType().Namespace);

            string ns = GetType().Namespace;

            context.MapRoute(
                AreaName,
                AreaName + "/{controller}/{action}",
                new { },
                new string[] { ns, ns + ".Shared" }
            );
        }
    }
}
