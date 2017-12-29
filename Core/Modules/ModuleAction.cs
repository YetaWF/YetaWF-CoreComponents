/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Newtonsoft.Json;
using System;
using YetaWF.Core.Localize;
using YetaWF.Core.Models;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Packages;
using YetaWF.Core.Pages;
using YetaWF.Core.Serializers;
using YetaWF.Core.Skins;
using YetaWF.Core.Support;
using YetaWF.Core.Views.Shared;
#if MVC6
using Microsoft.AspNetCore.Routing;
#else
using System.Web.Mvc;
using System.Web.Routing;
#endif

namespace YetaWF.Core.Modules {

    //TODO: There are many properties that should not be serialized
    [Serializable]
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

        public enum MenuEntryType {
            [EnumDescription("Regular Entry")]
            Entry = 0,
            [EnumDescription("Parent Entry (no Url or action available)")]
            Parent = 1,
            [EnumDescription("Separator")]
            Separator = 2,
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
        // we introduced a fake _Text property to avoid problems with the MultiString type when rendering in a treeview (as node text)
        [UIHint("Hidden"), DontSave]
        public string _Text { get { return MenuText.ToString(); } set { MenuText = value;} }// this is the same as the menu text (localized, for menu editing)

        [DontSave]// only used during menu editing
        [Caption("Entry Type"), Description("The type of the menu entry")]
        [UIHint("Enum")]
        public MenuEntryType EntryType {
            get {
                if (Separator) return MenuEntryType.Separator;
                if (string.IsNullOrWhiteSpace(Url) && SubModule == null) return MenuEntryType.Parent;
                return MenuEntryType.Entry;
            }
            set {
                switch (value) {
                    case MenuEntryType.Parent:
                        Url = "";
                        Separator = false;
                        break;
                    case MenuEntryType.Separator:
                        Separator = true;
                        break;
                    default:
                        break;
                }
            }
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

        public bool Separator { get; set; } // gap (if used, all other properties are ignored)

        [Caption("Url"), Description("The Url")]
        [UIHint("Url"), AdditionalMetadata("UrlType", UrlHelperEx.UrlTypeEnum.Local | UrlHelperEx.UrlTypeEnum.Remote), UrlValidation(UrlValidationAttribute.SchemaEnum.Any, UrlHelperEx.UrlTypeEnum.Local | UrlHelperEx.UrlTypeEnum.Remote)]
        [StringLength(Globals.MaxUrl), Trim]
        public string Url { get; set; } // The Url to cause this action

        [Caption("SubModule"),
            Description("The submodule is displayed as a complete submenu - " +
            "If a submodule is defined it replaces the entire submenu. " +
            "Submodules should not display forms as any popup message due to invalid input would close the submenu. " +
            "This is best used to display formatted links or images, etc. with a Text module. " +
            "Submodules are only supported with Bootstrap skins and are ignored on non-Bootstrap skins")]
        [UIHint("ModuleSelection")]
        public Guid? SubModule { get; set; }

        [Caption("Menu Text"), Description("The text shown for this menu entry when used as a menu entry")]
        [UIHint("MultiString20"), StringLength(MaxMenuText), RequiredIfNotAttribute("EntryType", (int) MenuEntryType.Separator)]
        public MultiString MenuText { get; set; }

        [Caption("Link Text"), Description("The text shown for this menu entry when used as a link")]
        [UIHint("MultiString20"), StringLength(MaxLinkText), RequiredIfNotAttribute("EntryType", (int) MenuEntryType.Separator)]
        public MultiString LinkText { get; set; }

        // Image is only used at runtime to set the image, which is immediately translated to a full path (ImageUrlFinal) for non-builtin icons
        // For built-in icons, we save the icon name
        [DontSave, ReadOnly, JsonIgnoreAttribute]
        public string Image {
            get {
                return ImageUrlFinal;
            }
            set {
                if (value != null) {
                    string img = value.Trim();
                    if (img.StartsWith("#")) {
                        ImageUrlFinal = img;
                    } else {
                        if (OwningModule == null) {
                            //throw new InternalError("need an owning module");
                            ImageUrlFinal = "#NotFound";
                        } else {
                            SkinImages skinImages = new SkinImages();
                            ImageUrlFinal = skinImages.FindIcon_Package(img, Package.GetCurrentPackage(OwningModule));
                        }
                    }
                } else {
                    ImageUrlFinal = null;
                }
            }
        }
        /// <summary>
        /// The saved image url or built-in name
        /// </summary>
        /// <remarks>Use GetImageUrlFinal() to retrieve full Url</remarks>
        [Caption("Image URL"), Description("The URL of the image shown for this entry")]
        [UIHint("Text80"), StringLength(Globals.MaxUrl)]
        public string ImageUrlFinal { get; set; }

        public string GetImageUrlFinal() {
            if (ImageUrlFinal != null && ImageUrlFinal.StartsWith("#")) {
                SkinImages skinImages = new SkinImages();
                return skinImages.FindIcon_Package(ImageUrlFinal, null);
            } else
                return ImageUrlFinal;
        }

        [Caption("Tooltip"), Description("The tooltip for this entry")]
        [UIHint("MultiString40"), StringLength(MaxTooltip)] //, RequiredIfAttribute("EntryType", (int) MenuEntryType.Entry)]
        public MultiString Tooltip { get; set; } // hover tooltip text

        [Caption("Legend"), Description("The legend for this entry")]
        [UIHint("MultiString40"), StringLength(MaxLegend)] //, RequiredIfAttribute("EntryType", (int) MenuEntryType.Entry)]
        public MultiString Legend { get; set; } // displayed to explain this and other commands  (usually in a list)

        [Caption("Enabled"), Description("Defines whether this entry is enabled - Disabled entries are automatically hidden in menus")]
        [UIHint("Boolean")]
        public bool Enabled { get; set; }

        [Caption("Css Class"), Description("The optional CSS class added to the action")]
        [UIHint("Text40"), StringLength(MaxCssClass)]
        public string CssClass { get; set; }

        [Caption("Style"), Description("Defines how this action affects the current window")]
        [UIHint("Enum"), RequiredIfAttribute("EntryType", (int)MenuEntryType.Entry)]
        public ActionStyleEnum Style { get; set; } // how the action affects the current window

        [Caption("Mode"), Description("Defines where this entry is available")]
        [UIHint("Enum"), RequiredIfAttribute("EntryType", (int)MenuEntryType.Entry)]
        public ActionModeEnum Mode { get; set; } // in which page mode the action is available

        [Caption("Category"), Description("The type of action this entry takes")]
        [UIHint("Enum"), RequiredIfAttribute("EntryType", (int)MenuEntryType.Entry)]
        public ActionCategoryEnum Category { get; set; } // the type of action taken

        [Caption("Limit To Role"), Description("Defines which role must be present for this action to be shown - This is normally only used for specialized entries which should only be shown in some cases but are available (by Url) to other roles also - This setting is ignored for superusers")]
        [UIHint("YetaWF_Identity_RoleId"), AdditionalMetadata("ShowDefault", true)]
        public int LimitToRole { get; set; } // the type of action taken

        [Caption("Ignore Authorization"), Description("Defines whether the target page's authorization is ignored - Actions are only visible if the user has sufficient authorization to perform the action - This can be used to force display of actions even when there is insufficient authoriation - For anonymous users this forces the user to log in first - This setting is ignored for links to other sites")]
        [UIHint("Boolean")]
        public bool AuthorizationIgnore { get; set; }

        [Description("The confirmation text displayed before the action takes place")]
        [Caption("Confirmation Text"), StringLength(MaxConfirmationText)]
        public MultiString ConfirmationText { get; set; } // confirmation popup text before action takes place

        [Description("If specified, the \"Please Wait\" dialog is shown when the action is selected - Only available for actions with Style=Normal")]
        [Caption("Please Wait"), StringLength(MaxPleaseWaitText)]
        public MultiString PleaseWaitText { get; set; }

        [Caption("Save Return Url"), Description("Defines whether this action will preserve the origin list (past Urls visited) and save the current Url - This is typically used for actions that display a form with a Cancel or Return button, which would return to the current Url if Save Return Url is selected")]
        [UIHint("Boolean")]
        public bool SaveReturnUrl { get; set; }

        [Caption("Add to Origin List"), Description("Defines whether the current Url will be added to the origin list so we can return to the current Url - This is used in conjunction with the Save Return Url property - If the Save Return Url property is false, the AddToOriginList property is ignored")]
        [UIHint("Boolean")]
        public bool AddToOriginList { get; set; }

        [Caption("Needs Module Context"), Description("The whether module context is required for authorization purposes - This is defined by the action")]
        [UIHint("Boolean")]
        public bool NeedsModuleContext { get; set; }

        [Caption("Don't Follow"), Description("Defines whether search engines and bots follow this link (select to disable)")]
        [UIHint("Boolean")]
        public bool DontFollow { get; set; }

        /// <summary>
        /// Name used in html to identify the action
        /// </summary>
        [JsonIgnoreAttribute]
        public string Name { get; set; }
        /// <summary>
        /// Used in html to determine the initial display
        /// </summary>
        [JsonIgnoreAttribute]
        public bool Displayed { get; set; }
        /// <summary>
        /// Used in html and rendered as data-extradata attribute.
        /// </summary>
        /// <remarks>This could be used to pass additional data to client-side processing of this action, typically used with ActionStyleEnum.Nothing.</remarks>
        [JsonIgnoreAttribute, DontSave]
        public string ExtraData { get; set; }

        // in a GET request use a cookie as a signal that the data has been sent
        // this is normally used in <a> links that are used to download data (like zip files)
        // so the "Loading" animation can be stopped
        [JsonIgnoreAttribute]
        public bool CookieAsDoneSignal { get; set; }

        [JsonIgnoreAttribute]
        public ActionLocationEnum Location { get; set; } // the type of menu where that action is shown

        public SerializableList<ModuleAction> SubMenu { get; set; } // submenu

        [JsonIgnoreAttribute] // menus don't support queryargs - they can be encoded as part of the url
        public object QueryArgs { get; set; } // arguments
        [JsonIgnoreAttribute]
        [Obsolete("Do not use - replaced by QueryArgsDict")]
        public RouteValueDictionary QueryArgsRvd { get; set; }
        [JsonIgnoreAttribute]
        public QueryHelper QueryArgsDict { get; set; }
        [JsonIgnoreAttribute] // menus don't support queryargshr - they can be encoded as part of the url
        public object QueryArgsHR { get; set; } // arguments part of URL as human readable parts
        [JsonIgnoreAttribute] // anchor used as part of URL
        public string AnchorId { get; set; }

        [JsonIgnoreAttribute]// This is set in IsAuthorized and it is not user-definable
        public PageDefinition.PageSecurityType PageSecurity { get; set; }

        [JsonIgnoreAttribute]
        public bool DontCheckAuthorization { get; set; }// don't check whether user is authorized (always show) - this will force a login/register when used

        [Obsolete("Discontinued - preserve property so deserializing existing data doesn't fail")]
        // Discontinued: we have to use "items" because kendo treeview doesn't let us to use a different variable name - we're no longer using kendo treeview
        [JsonIgnoreAttribute]
        public SerializableList<ModuleAction> items {
            get {  return null; } set { }
        }

        public int Id { get; set; } // ids are used for editing purposes to match up old and new menu entries

        // GetUserMenu evaluates all ModuleActions so their authorization doesn't have to be reevaluated
        [JsonIgnoreAttribute]
        public bool _AuthorizationEvaluated { get; set; }

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
    }
}
