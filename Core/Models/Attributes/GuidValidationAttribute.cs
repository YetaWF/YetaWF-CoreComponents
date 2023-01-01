/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using YetaWF.Core.Localize;

namespace YetaWF.Core.Models.Attributes {

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class GuidValidationAttribute : RegexValidationBaseAttribute {

        private static string __ResStr(string name, string defaultValue, params object?[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public GuidValidationAttribute() : base(@"^\s*([a-fA-F0-9]{8}\-[a-fA-F0-9]{4}\-[a-fA-F0-9]{4}\-[a-fA-F0-9]{4}\-[a-fA-F0-9]{12})\s*$",
                __ResStr("valGuid", "The guid is invalid - it should be in the format '00000000-0000-0000-0000-000000000000'"),
                __ResStr("valGuid2", "The guid (field '{0}') is invalid - it should be in the format '00000000-0000-0000-0000-000000000000'"),
                __ResStr("valGuid3", "The guid '{0}' is invalid - it should be in the format '00000000-0000-0000-0000-000000000000'")
            ) { }
    }
}
