/* Copyright © 2021 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YetaWF.Core.Support;

namespace YetaWF.Core.Models {

    /// <summary>
    /// An instance of this class describes a tab control. The implementation of the tab control is deferred to a component provider.
    /// </summary>
    public class TabsDefinition {

        // set up by application
        public List<TabEntry> Tabs { get; set; }
        public bool ContextMenu { get; set; }

        // other settings
        public string Id { get; set; } // html id of the tab control
        public int ActiveTabIndex { get; set; }

        public TabsDefinition() {
            Tabs = new List<TabEntry>();
            ContextMenu = false;
            ActiveTabIndex = 0;
            Id = YetaWFManager.Manager.UniqueId("tab");
        }
    }

    /// <summary>
    /// Describes one individual tab and the associated optional pane.
    /// </summary>
    public class TabEntry {
        public MultiString? Caption { get; set; }
        public MultiString? ToolTip { get; set; }
        public string? TabCssClasses { get; set; }
        public string? PaneCssClasses { get; set; }
        public Func<int, Task<string>>? RenderPaneAsync { get; set; }
    }
}
