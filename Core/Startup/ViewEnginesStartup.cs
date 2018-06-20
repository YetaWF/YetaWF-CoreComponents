/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Linq;
#if MVC6
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;
#else
using System.Web.Mvc;
#endif
using YetaWF.Core.Log;
using YetaWF.Core.Packages;

namespace YetaWF.Core.Support {

    public static class ViewEnginesStartup {

        /// <summary>
        /// Update view engines so we can find our views.
        /// </summary>
#if MVC6
        public static void Start(IServiceCollection services) {

            services.Configure<RazorViewEngineOptions>(options => {
                options.AreaViewLocationFormats.Clear();
            });
        }
#else
        public static void Start() {
            Logging.AddLog("Establishing ViewEngines");

            // Add global usings for razor pages
            //System.Web.WebPages.Razor.WebPageRazorHost.AddGlobalImport("System.Web.Mvc.Html");

            // remove any non razor precompiled engines
            ViewEngines.Engines.Clear();

            // Add a view engine that searches areas
            RazorViewEngine engine = new RazorViewEngine {
                AreaMasterLocationFormats = new string[] { },
                MasterLocationFormats = new string[] { },
                FileExtensions = new string[] { "cshtml" },
                AreaPartialViewLocationFormats = new string[] { }, // we're not using partial views
                AreaViewLocationFormats = new string[] { }, // we're not using views
                PartialViewLocationFormats = new string[] { }, // we're not using partial views
                ViewLocationFormats = new string[] { }, // we're not using views
            };
            ViewEngines.Engines.Add(engine);
        }
#endif
    }
}


