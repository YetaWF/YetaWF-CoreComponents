/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;

namespace YetaWF.Core.Modules {
    public static class ModuleActionHelper {
        public static void New(this List<ModuleAction> actions, ModuleAction action) {
            if (action == null) return;
            actions.Add(action);
        }
    }
}
