/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using YetaWF.Core.Support;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace YetaWF.Core.Controllers {

    /// <summary>
    /// A JSON action result which includes an Access-Control-Allow-Origin header.
    /// </summary>
    public class CorsJsonResult : YJsonResult {

        /// <summary>
        /// Defines the allowable target domain. Specify "*" for all domains.
        /// </summary>
        public string? TargetDomain { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public CorsJsonResult() { }

        /// <summary>
        /// Processes the action result.
        /// </summary>
        /// <param name="context">The action context.</param>
        public override Task ExecuteResultAsync(ActionContext context) {
            context.HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", string.IsNullOrWhiteSpace(TargetDomain) ? "*" : TargetDomain.ToLower());
            return base.ExecuteResultAsync(context);
        }
    }
}
