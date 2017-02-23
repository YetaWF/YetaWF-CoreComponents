/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using YetaWF.Core.Support;
#if MVC6
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
#else
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Controllers {

    public class CorsJsonResult : YJsonResult {

        public string TargetDomain { get; set; }

        public CorsJsonResult() { }
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
