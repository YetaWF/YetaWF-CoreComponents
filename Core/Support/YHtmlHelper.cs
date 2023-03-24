﻿/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using YetaWF.Core.Endpoints.Support;

namespace YetaWF.Core.Support {

    /// <summary>
    /// A class implementing a minimal HtmlHelper after removing Razor.
    /// HtmlHelper in MVC is an abomination with a gazillion extension methods.
    /// Since we dropped Razor, we don't need it.
    /// </summary>
    public class YHtmlHelper {

        public ModelState ModelState { get; private set; }

        public YHtmlHelper(ModelState? modelState) {
            ModelState = modelState ?? new ModelState();
        }
    }
}

