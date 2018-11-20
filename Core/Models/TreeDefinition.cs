﻿/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using YetaWF.Core.Localize;
using YetaWF.Core.Support;

namespace YetaWF.Core.Models {

    public class TreeDefinition {

        // set up by application
        public Type RecordType { get; set; }
        public bool ShowHeader { get; set; }

        public bool DragDrop { get; set; }
        public string NoRecordsText { get; set; }// text shown when there are no records
        public bool UseSkinFormatting { get; set; } // use skin theme (jquery-ui)

        // other settings
        public string Id { get; set; } // html id of the grid

        public TreeDefinition() {

            ShowHeader = true;
            DragDrop = false;
            NoRecordsText = this.__ResStr("noRecs", "(None)");
            UseSkinFormatting = true;

            Id = YetaWFManager.Manager.UniqueId("tree");
        }
    }
}
