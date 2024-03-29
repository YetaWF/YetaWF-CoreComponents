﻿/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

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
        public Type RecordType { get; set; } = null!;

        public bool ShowHeader { get; set; }
        public MultiString Header { get; set; }
        public MultiString HeaderTooltip { get; set; }

        public bool DragDrop { get; set; }
        public bool ContextMenu { get; set; } // Supports context menu
        public string NoRecordsText { get; set; }// text shown when there are no records

        public string? AjaxUrl { get; set; } // for dynamic population during expand
        public bool JSONData { get; set; } // defines whether JSON data is attached to each entry

        // other settings
        public string Id { get; set; } // html id of the tree

        public TreeDefinition() {
            ShowHeader = true;
            DragDrop = false;
            ContextMenu = false;
            NoRecordsText = this.__ResStr("noRecs", "(None)");
            JSONData = false;

            Id = YetaWFManager.Manager.UniqueId("tree");

            Header = new MultiString();
            HeaderTooltip = new MultiString();
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
        public TreeDefinition TreeDef { get; set; } = null!;
        /// <summary>
        /// The collection of data to be rendered.
        /// </summary>
        public DataSourceResult Data { get; set; } = null!;
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
        public List<TreeEntry>? SubEntries { get; set; }

        /// <summary>
        /// Determines whether an item's subitems are dynamically added/removed.
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
        public virtual string Text { get; set; } = null!;

        /// <summary>
        /// The item's additional CSS added to the link generated for the entry.
        /// </summary>
        [JsonIgnore]
        public virtual string? ExtraCss { get; set; }

        /// <summary>
        /// Optional. The item's text display ahead of the text. Must render as an &lt;a&gt; tag.
        /// </summary>
        [JsonIgnore]
        public virtual object? BeforeText { get; set; }

        /// <summary>
        /// Optional. The item's text display after the text. Must render as an &lt;a&gt; tag.
        /// </summary>
        [JsonIgnore]
        public virtual object? AfterText { get; set; }

        /// <summary>
        /// Used as the item's target URL, opened in a new window.
        /// </summary>
        [JsonIgnore]
        public virtual string? UrlNew { get; set; }
        /// <summary>
        /// Used as the item's target URL, used to replace a content pane or the entire page if no content information is available.
        /// </summary>
        [JsonIgnore]
        public virtual string? UrlContent { get; set; }
    }

}
