/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using YetaWF.Core.Components;
using YetaWF.Core.Localize;
using YetaWF.Core.Support;

namespace YetaWF.Core.Models {

    /// <summary>
    /// An instance of this class describes a tree. The implementation of the tree is deferred to a component provider.
    /// </summary>
    public class TreeDefinition {

        // set up by application
        public Type RecordType { get; set; }

        public bool ShowHeader { get; set; }

        public bool DragDrop { get; set; }
        public string NoRecordsText { get; set; }// text shown when there are no records
        public bool UseSkinFormatting { get; set; } // use skin theme (jquery-ui)

        public string ContentTargetId { get; set; }
        public string ContentTargetPane { get; set; }
        public string AjaxUrl { get; set; } // for dynamic population during expand
        public bool JSONData { get; set; } // defines whether JSON data is attached to each entry

        // other settings
        public string Id { get; set; } // html id of the tree

        public TreeDefinition() {
            ShowHeader = true;
            DragDrop = false;
            NoRecordsText = this.__ResStr("noRecs", "(None)");
            UseSkinFormatting = true;
            JSONData = false;

            Id = YetaWFManager.Manager.UniqueId("tree");
        }
    }

    /// <summary>
    /// Describes the records to be rendered for a tree control.
    ///
    /// This class is not used by applications. It is reserved for component implementation.
    /// An instance of the GridPartialData class defines all data to be rendered to replace a tree component's contents.
    /// The implementation of rendering the tree data is deferred to a component provider.
    /// </summary>
    public class TreePartialData {
        /// <summary>
        /// The GridDefinition object describing the current grid.
        /// </summary>
        public TreeDefinition TreeDef { get; set; }
        /// <summary>
        /// The collection of data to be rendered.
        /// </summary>
        public DataSourceResult Data { get; set; }
    }

    /// <summary>
    /// The type of link used by a menu entry.
    /// </summary>
    public enum LinkTypeEnum {
        /// <summary>
        /// UrlContent property has a local link.
        /// </summary>
        Local = 0,
        /// <summary>
        /// UrlNew has an external link.
        /// </summary>
        External = 1,
    }

    /// <summary>
    /// Base class for all tree entries.
    /// </summary>
    public abstract class TreeEntry {

        /// <summary>
        /// Constructor.
        /// </summary>
        public TreeEntry() {
            SubEntries = new List<TreeEntry>();
        }

        /// <summary>
        /// The type of link used by the entry.
        /// </summary>
        [JsonIgnore]
        public LinkTypeEnum LinkType { get; set; }

        /// <summary>
        /// The item's collection of subitems or null if the item has no subitems.
        /// </summary>
        [JsonIgnore]
        public List<TreeEntry> SubEntries { get; set; }

        /// <summary>
        /// Determines whether an item's subsitems are dynamically added/removed.
        /// </summary>
        public virtual bool DynamicSubEntries { get; set; }

        /// <summary>
        /// Determines whether the item should be rendered collapsed (true) or expanded (false).
        /// </summary>
        [JsonIgnore]
        public virtual bool Collapsed { get; set; }

        /// <summary>
        /// Determines whether the item should be rendered as initially selected.
        /// </summary>
        [JsonIgnore]
        public virtual bool Selected { get; set; }

        /// <summary>
        /// The item's displayed text. Must be a string, not a complex component.
        /// </summary>
        [JsonIgnore]
        public virtual string Text { get; set; }

        /// <summary>
        /// Optional. The item's text display ahead of the text. Must render as an <a> tag.
        /// </summary>
        [JsonIgnore]
        public virtual object BeforeText { get; set; }

        /// <summary>
        /// Optional. The item's text display after the text. Must render as an <a> tag.
        /// </summary>
        [JsonIgnore]
        public virtual object AfterText { get; set; }

        /// <summary>
        /// Used as the item's target URL, opened in a new window.
        /// </summary>
        [JsonIgnore]
        public virtual string UrlNew { get; set; }
        /// <summary>
        /// Used as the item's target URL, used to replace a content pane or the entire page if no content information is available.
        /// </summary>
        [JsonIgnore]
        public virtual string UrlContent { get; set; }
    }

}
