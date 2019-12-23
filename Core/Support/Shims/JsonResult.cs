/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

#if MVC6
using Microsoft.AspNetCore.Mvc;
#else
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Support {

    public class YJsonResult : JsonResult {

#if MVC6
        public YJsonResult() : base(null) {
            SerializerSettings = new Newtonsoft.Json.JsonSerializerSettings() {
                ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver {
                    NamingStrategy = new Newtonsoft.Json.Serialization.DefaultNamingStrategy()
                },
            };
        }
        public object Data { get { return Value; }  set { Value = value; } }
#else
#endif
    }
}

