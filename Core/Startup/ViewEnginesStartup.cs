/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

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
                options.AreaViewLocationFormats.Add("/Areas/{2}/Views/Shared/{0}.cshtml");
                options.AreaViewLocationFormats.Add("/Areas/{2}/Views/{0}.cshtml");
                List<string> publicAreaPartialViews = GetPublicAreaPartialViews();
                foreach (string fmt in publicAreaPartialViews)
                    options.AreaViewLocationFormats.Add(fmt);
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
                AreaPartialViewLocationFormats = new string[] {
                    "~/Areas/{2}/Views/Shared/{0}.cshtml",
                    "~/Areas/{2}/Views/{0}.cshtml",
                },
                AreaViewLocationFormats = new string[] {
                    "~/Areas/{2}/Views/{0}.cshtml",
                    "~/Areas/YetaWF_Core/Views/Shared/{0}.cshtml", // ShowMessage
                },
                PartialViewLocationFormats = new string[] { },
                ViewLocationFormats = new string[] { },
            };
            AddAreas(engine); // add areas that expose partial views to everyone
            ViewEngines.Engines.Add(engine);
        }
        private static void AddAreas(RazorViewEngine engine) {

            List<string> publicAreaPartialViews = GetPublicAreaPartialViews();

            List<string> pvs = engine.PartialViewLocationFormats.ToList();
            pvs.AddRange(publicAreaPartialViews);
            engine.PartialViewLocationFormats = pvs.ToArray();

            //pvs = engine.AreaPartialViewLocationFormats.ToList();
            //pvs.AddRange(publicAreaPartialViews);
            //engine.AreaPartialViewLocationFormats = pvs.ToArray();
        }
#endif
        private static List<string> GetPublicAreaPartialViews() {
#if MVC6
#else
            Logging.AddLog("Processing Public Area Partial Views");
#endif
            List<string> pvs = new List<string>();
            List<Package> packages = Package.GetAvailablePackages();
            foreach (Package package in packages) {
                if (package.HasPublicPartialViews) {
#if MVC6
                    pvs.Add(string.Format("/Areas/{0}/Views/Shared/{{0}}.cshtml", package.AreaName));
#else
                    pvs.Add(string.Format("~/Areas/{0}/Views/Shared/{{0}}.cshtml", package.AreaName));
                    Logging.AddLog("Found {0}", package.AreaName);
#endif
                }
            }
#if MVC6
#else
            Logging.AddLog("Processing Public Area Partial Views Ended");
#endif
            return pvs;
        }
    }
}


