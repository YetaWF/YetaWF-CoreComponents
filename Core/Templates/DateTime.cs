﻿/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Threading.Tasks;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Addons.Templates {
    public class DateTime : IAddOnSupport {

        public Task AddSupportAsync(YetaWFManager manager) {

            ScriptManager scripts = manager.ScriptManager;

            scripts.AddVolatileOption("DateTime", "DateTimeFormat", YetaWF.Core.Localize.Formatting.GetFormatDateTimeFormat());

            return Task.CompletedTask;
        }
    }
}
