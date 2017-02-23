/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

#if MVC6
using Microsoft.AspNetCore.Mvc;
#else
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Support {

    public class YJsonResult : JsonResult {

#if MVC6
        public YJsonResult() : base(null) { }
        public object Data { get { return Value; }  set { Value = value; } }
#else
#endif
    }
}

