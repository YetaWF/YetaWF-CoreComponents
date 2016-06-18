/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System.Web.Mvc;

namespace YetaWF.Core.Controllers {

    public class CorsJsonResult : JsonResult {

        public string TargetDomain { get; set; }

        public CorsJsonResult() { }

        public override void ExecuteResult(ControllerContext context) {
            context.HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", string.IsNullOrWhiteSpace(TargetDomain) ? "*" : TargetDomain.ToLower());
            //testing only. context.HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            base.ExecuteResult(context);
        }
    }
}
