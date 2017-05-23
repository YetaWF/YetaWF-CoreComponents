/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using YetaWF.Core.Support;
#if MVC6
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
#else
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Controllers {

    /// <summary>
    /// A Json action result which includes an Access-Control-Allow-Origin header.
    /// </summary>
    public class CorsJsonResult : YJsonResult {

        /// <summary>
        /// Defines the allowable target domain. Specify "*" for all domains.
        /// </summary>
        public string TargetDomain { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public CorsJsonResult() { }

        /// <summary>
        /// Processes the action result.
        /// </summary>
        /// <param name="context">The action context.</param>
#if MVC6
        public override Task ExecuteResultAsync(ActionContext context) {
            context.HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", string.IsNullOrWhiteSpace(TargetDomain) ? "*" : TargetDomain.ToLower());
            return base.ExecuteResultAsync(context);
        }
#else
        public override void ExecuteResult(ControllerContext context) {
            context.HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", string.IsNullOrWhiteSpace(TargetDomain) ? "*" : TargetDomain.ToLower());
            //testing only. context.HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            base.ExecuteResult(context);
        }
#endif
    }
}
