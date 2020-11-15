/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

#nullable enable

using System.Collections.Generic;

namespace YetaWF.Core.Modules {

    public static class ModuleActionHelper {

        public static void New(this List<ModuleAction> actions, ModuleAction action) {
            if (action == null) return;
            actions.Add(action);
        }

        public static ModuleAction BuiltIn_ExpandAction(string text, string? tooltip = null) {
            ModuleAction action = new ModuleAction {
                Name = "Expand",
                Category = ModuleAction.ActionCategoryEnum.Read,
                CssClass = "",
                Image = "#Expand",
                Location = ModuleAction.ActionLocationEnum.Any,
                Mode = ModuleAction.ActionModeEnum.Any,
                Style = ModuleAction.ActionStyleEnum.Nothing,
                LinkText = text,
                MenuText = new Models.MultiString(),
                Tooltip = tooltip,
                Legend = new Models.MultiString(),
            };
            return action;
        }
        public static ModuleAction BuiltIn_CollapseAction(string text, string? tooltip = null) {
            ModuleAction action = new ModuleAction {
                Name = "Collapse",
                Category = ModuleAction.ActionCategoryEnum.Read,
                CssClass = "",
                Image = "#Collapse",
                Location = ModuleAction.ActionLocationEnum.Any,
                Mode = ModuleAction.ActionModeEnum.Any,
                Style = ModuleAction.ActionStyleEnum.Nothing,
                LinkText = text,
                MenuText = new Models.MultiString(),
                Tooltip = tooltip,
                Legend = new Models.MultiString(),
            };
            return action;
        }
    }
}
