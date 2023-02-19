/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace YetaWF.Core.Pages {

    public class ActionInfo {
        public string HTML { get; set; } = null!;
        public bool Failed { get; set; }

        public static ActionInfo Empty => new ActionInfo { HTML = string.Empty, Failed = false };
    }
}
