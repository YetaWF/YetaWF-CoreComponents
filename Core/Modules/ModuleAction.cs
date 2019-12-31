/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using YetaWF.Core.Localize;
using YetaWF.Core.Models;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Pages;
using YetaWF.Core.Serializers;
using YetaWF.Core.Support;
#if MVC6
using Microsoft.AspNetCore.Routing;
#else
using System.Web.Mvc;
using System.Web.Routing;
#endif

namespace YetaWF.Core.Modules {

    public partial class ModuleAction {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(ModuleAction), name, defaultValue, parms); }

        protected static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        public const int MaxTooltip = 100;
        public const int MaxLegend = 100;
        public const int MaxConfirmationText = 200;
        public const int MaxPleaseWaitText = 200;
        public const int MaxCssClass = 40;
        public const int MaxMenuText = 50;
        public const int MaxLinkText = 50;

        public enum MenuEntryType {
            [EnumDescription("Regular Entry")]
            Entry = 0,
            [EnumDescription("Parent Entry (no Url or action available)")]
            Parent = 1,
            [EnumDescription("Separator")]
            Separator = 2,
        }

        public enum ActionStyleEnum {
            [EnumDescription("Normal", "Navigate to page within the same window")]
            Normal = 0,
            [EnumDescription("New Page", "Opens a new window")]
            NewWindow = 1,
            [EnumDescription("Popup", "Opens page in a popup window (in view mode only) - If a popup window is already active, it opens a new window instead")]
            Popup = 2,
            [EnumDescription("Popup Edit", "Opens page in a popup window (in view and edit mode - generally used for system modules only) - If a popup window is already active, it opens a new window instead")]
            PopupEdit = 3,
            [EnumDescription("Force Popup", "Opens page in a popup window (in view mode only) - If a popup window is already active, the current popup window is replaced by this new popup window")]
            ForcePopup = 4,
            [EnumDescription("Post", "Requires non-page response (JavaScript) suitable for display in a popup (success/fail)")]
            Post = 5,
            [EnumDescription("Nothing", "Generates all HTML but takes no action when clicked (typically used for JavaScript/client side control)")]
            Nothing = 6,
            [EnumDescription("Outer Window", "Used in a popup so the action affects the parent (main) window")]
            OuterWindow = 7,
        }

        public enum ActionModeEnum {
            [EnumDescription("Edit and view mode")]
            Any = 0,
            [EnumDescription("View mode")]
            View = 1,
            [EnumDescription("Edit mode")]
            Edit = 2,
        }

        [Flags]
        public enum ActionLocationEnum {
            /// <summary>
            /// In main action menu
            /// </summary>
            MainMenu = 0x0001,
            /// <summary>
            /// In module menu
            /// </summary>
            ModuleMenu = 0x0002,
            /// <summary>
            /// In link menu
            /// </summary>
            ModuleLinks = 0x0004,
            /// <summary>
            /// Grid (always explicit)
            /// </summary>
            GridLinks = 0x0008,
            Explicit = 0x0008,

            /// <summary>
            /// In any menu/links (excludes popup)
            /// </summary>
            Any = MainMenu|ModuleMenu|ModuleLinks|GridLinks,
            /// <summary>
            /// In any menu (not links)
            /// </summary>
            AnyMenu = ModuleMenu | MainMenu,

            /// <summary>
            /// Don't add this automatically to a module menu
            /// </summary>
            NoAuto = 0x0080,

            /// <summary>
            /// Allow in a popup (rarely used)
            /// </summary>
            InPopup = 0x0100,
        }
        public enum ActionCategoryEnum {
            /// <summary>
            /// The action doesn't update anything
            /// </summary>
            Read = 0,
            /// <summary>
            /// The action updates something
            /// </summary>
            Update = 1,
            /// <summary>
            /// The action is destructive
            /// </summary>
            Delete = 2,
            /// <summary>
            /// Something really important
            /// </summary>
            Significant = 999, // always prompt
        }

        public ModuleAction() {
            Separator = false;
            Url = null;
            SubModule = null;
            MenuText = __ResStr("MenuText", "(New)");
            LinkText = __ResStr("LinkText", "(New)");
            ImageUrlFinal = null;
            Tooltip = new MultiString();
            Legend = new MultiString();
            Enabled = true;
            Style = ActionStyleEnum.Normal;
            Mode = ActionModeEnum.Any;
            Category = ActionCategoryEnum.Read;
            LimitToRole = 0;
            AuthorizationIgnore = false;
            ConfirmationText = new MultiString();
            PleaseWaitText = new MultiString();
            SaveReturnUrl = false;
            AddToOriginList = true;
            NeedsModuleContext = false;
            DontFollow = false;

            Displayed = true;
            CookieAsDoneSignal = false;
            Location = ActionLocationEnum.Any;
            QueryArgs = null;
            QueryArgsDict = null;
            _AuthorizationEvaluated = false;
            OwningModule = null;
            PageSecurity = PageDefinition.PageSecurityType.Any;
        }

        public ModuleAction(ModuleDefinition owningModule) : this() {
            OwningModule = owningModule;
        }

        public MenuEntryType EntryType {
            get {
                if (Separator) return MenuEntryType.Separator;
                if (string.IsNullOrWhiteSpace(Url) && SubModule == null) return MenuEntryType.Parent;
                return MenuEntryType.Entry;
            }
        }
        public bool Separator { get; set; } // gap (if used, all other properties are ignored)

        [StringLength(Globals.MaxUrl)]
        public string Url { get; set; } // The Url to cause this action

        public Guid? SubModule { get; set; }

        [StringLength(MaxMenuText)]
        public MultiString MenuText { get; set; }

        [StringLength(MaxLinkText)]
        public MultiString LinkText { get; set; }

        // Image is only used at runtime to set the image, which is immediately translated to a full path (ImageUrlFinal) for non-builtin icons
        // For built-in icons, we save the icon name
        [DontSave, ReadOnly]
        public string Image {
            get { return ImageUrlFinal; }
            set { ImageUrlFinal = value; }
        }
        /// <summary>
        /// The saved image url or built-in name
        /// </summary>
        [StringLength(Globals.MaxUrl)]
        public string ImageUrlFinal { get; set; }

        [StringLength(MaxTooltip)]
        public MultiString Tooltip { get; set; } // hover tooltip text

        [StringLength(MaxLegend)]
        public MultiString Legend { get; set; } // displayed to explain this and other commands  (usually in a list)

        public bool Enabled { get; set; }

        [StringLength(MaxCssClass)]
        public string CssClass { get; set; }

        public ActionStyleEnum Style { get; set; } // how the action affects the current window

        public ActionModeEnum Mode { get; set; } // in which page mode the action is available

        public ActionCategoryEnum Category { get; set; } // the type of action taken

        public int LimitToRole { get; set; } // the type of action taken

        public bool AuthorizationIgnore { get; set; }

        [StringLength(MaxConfirmationText)]
        public MultiString ConfirmationText { get; set; } // confirmation popup text before action takes place

        [StringLength(MaxPleaseWaitText)]
        public MultiString PleaseWaitText { get; set; }

        public bool SaveReturnUrl { get; set; }

        public bool AddToOriginList { get; set; }

        public bool NeedsModuleContext { get; set; }

        public bool DontFollow { get; set; }

        /// <summary>
        /// Name used in html to identify the action
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Used in html to determine the initial display
        /// </summary>
        public bool Displayed { get; set; }
        /// <summary>
        /// Used in html and rendered as data-extradata attribute.
        /// </summary>
        /// <remarks>This could be used to pass additional data to client-side processing of this action, typically used with ActionStyleEnum.Nothing.</remarks>
        [DontSave]
        public string ExtraData { get; set; }

        // in a GET request use a cookie as a signal that the data has been sent
        // this is normally used in <a> links that are used to download data (like zip files)
        // so the "Loading" animation can be stopped
        public bool CookieAsDoneSignal { get; set; }

        // GetUserMenu evaluates all ModuleActions so their authorization doesn't have to be reevaluated
        [DontSave]
        public bool _AuthorizationEvaluated { get; set; }

        public ActionLocationEnum Location { get; set; } // the type of menu where that action is shown

        public SerializableList<ModuleAction> SubMenu { get; set; } // submenu

        // menus don't support queryargs - they can be encoded as part of the url
        public object QueryArgs { get; set; } // arguments
        [Obsolete("Do not use - replaced by QueryArgsDict")]
        public RouteValueDictionary QueryArgsRvd { get; set; }
        public QueryHelper QueryArgsDict { get; set; }
        // menus don't support queryargshr - they can be encoded as part of the url
        public object QueryArgsHR { get; set; } // arguments part of URL as human readable parts
        // anchor used as part of URL
        public string AnchorId { get; set; }

        // This is set in IsAuthorized and it is not user-definable
        public PageDefinition.PageSecurityType PageSecurity { get; set; }

        public bool DontCheckAuthorization { get; set; }// don't check whether user is authorized (always show) - this will force a login/register when used

        public Guid GetOwningModuleGuid() {
            if (OwningModuleGuid == Guid.Empty) {
                if (OwningModule == null)
                    throw new InternalError("Need OwningModule");
                OwningModuleGuid = OwningModule.ModuleGuid;
            }
            return OwningModuleGuid;
        }
        [DontSave, ReadOnly]// THIS IS STRICTLY USED FOR SERIALIZATION - DO NOT ACCESS DIRECTLY
        public Guid OwningModuleGuid { get; set; }
        private ModuleDefinition OwningModule { get; set; }

        [Obsolete("Discontinued - preserve property so deserializing existing data doesn't fail")]
        // Discontinued: we have to use "items" because kendo treeview doesn't let us to use a different variable name - we're no longer using kendo treeview
        public SerializableList<ModuleAction> items {
            get { return null; }
            set { }
        }
        [Obsolete("Discontinued - preserve property so deserializing existing data doesn't fail")]
        public int Id { get; set; }
    }
}
