/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Mvc;

namespace YetaWF.Core.Support {

    public class YJsonResult : JsonResult {

        public YJsonResult() : base(null) {
            SerializerSettings = new Newtonsoft.Json.JsonSerializerSettings() {
                ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver {
                    NamingStrategy = new Newtonsoft.Json.Serialization.DefaultNamingStrategy()
                },
            };
        }
        public object Data { get { return Value; }  set { Value = value; } }
    }
}

