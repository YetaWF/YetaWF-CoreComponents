/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using YetaWF.Core.Support;
using System.Collections.Generic;
using YetaWF.Core.Models.Attributes;
#if MVC6
using YetaWF.Core.Pages;
using Microsoft.AspNetCore.Mvc;
#else
using System.Web;
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Controllers {

    /// <summary>
    /// Controller for all page requests within YetaWF that only need addons rendered (used client-side to bootstrap additional missing addons, i.e., progressively populating JavaScript/CSS).
    /// </summary>
    /// <remarks>This controller is a plain MVC controller because we don't want any startup processing to take place (like authorization, etc.)
    /// because we handle all this here.</remarks>
    [AreaConvention]
    public class AddonContentController : Controller {

        internal static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        internal class Payload {
            public string Name { get; set; }
            public string Text { get; set; }
        }
        internal class UrlEntry {
            public string Name { get; set; }
            public string Url { get; set; }
        }
        public class AddonDescription {
            public string AreaName { get; set; }
            public string ShortName { get; set; }
            public string Argument1 { get; set; }
        }

        /// <summary>
        /// Data received from the client for the requested page.
        /// </summary>
        /// <remarks>An instance of this class is sent from the client to request a "Single Page Application" update to change from the current page URL to the requested URL.</remarks>
        public class DataIn {
            /// <summary>
            /// The requested addons.
            /// </summary>
            public List<AddonDescription> Addons { get; set; }
            /// <summary>
            /// A collection of all CSS files the client has already loaded.
            /// </summary>
            public List<string> KnownCss { get; set; }
            /// <summary>
            /// A collection of all JavaScript files the client has already loaded.
            /// </summary>
            public List<string> KnownScripts { get; set; }
        }

        /// <summary>
        /// The ShowAddons action handles all addon content requests issued client-side.
        /// </summary>
        /// <param name="dataIn">Describes the data requested.</param>
        /// <returns></returns>
        [AllowGet]
        public ActionResult ShowAddons([FromBody] DataIn dataIn) {

            if (!YetaWFManager.HaveManager || dataIn.Addons == null || (Manager.CurrentRequest.Headers == null || Manager.CurrentRequest.Headers["X-Requested-With"] != "XMLHttpRequest")) {
#if MVC6
                return new NotFoundObjectResult(null);
#else
                throw new HttpException(404, "Not found");
#endif
            }

            // Process the requested addons
            PageContentController.PageContentResult cr = new PageContentController.PageContentResult();
            return new AddonContentViewResult(dataIn);
        }
    }
}
