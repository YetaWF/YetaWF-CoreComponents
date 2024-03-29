﻿/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YetaWF.Core.Components;
using YetaWF.Core.DataProvider.Attributes;
using YetaWF.Core.Identity;
using YetaWF.Core.Models;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Pages;
using YetaWF.Core.Serializers;
using YetaWF.Core.Site;
using YetaWF.Core.Skins;
using YetaWF.Core.Support;

namespace YetaWF.Core.Modules {  // This namespace breaks naming standards so it can properly return module company/name for localization

    [ModuleGuidAttribute("00000000-0000-0000-0000-000000000000")]
    [Trim]
    public partial class ModuleDefinition {

        /* private static string __ResStr(string name, string defaultValue, params object?[] parms) { return ResourceAccess.GetResourceString(typeof(ModuleDefinition), name, defaultValue, parms); } */

        public const int MaxName = 80;
        public const int MaxDescription = 200;
        public const int MaxTitle = 100;
        public const int MaxCssClass = 100;
        public const int MaxHtmlId = 100;

        // STANDARD MODULE VIEW
        // STANDARD MODULE VIEW
        // STANDARD MODULE VIEW

        public class StandardViews {
            /// <summary>
            /// Configuration view with Apply/Save/Cancel buttons.
            /// </summary>
            public const string Config = EditApply;
            /// <summary>
            /// Browse view without buttons.
            /// </summary>
            public const string Browse = "Browse";
            /// <summary>
            /// Add view with Save/Cancel buttons.
            /// </summary>
            public const string Add = "Add";
            /// <summary>
            /// Edit view with Save/Cancel buttons.
            /// </summary>
            public const string Edit = "Edit";
            /// <summary>
            /// Edit view with Apply/Save/Cancel buttons.
            /// </summary>
            public const string EditApply = "EditApply";
            /// <summary>
            /// Display view with Cancel button.
            /// </summary>
            public const string Display = "Display";

            /// <summary>
            /// A view with a read/only PropertyList without any buttons.
            /// </summary>
            public const string PropertyListDisplay = "PropertyList_Display";
            /// <summary>
            /// A view with an editable PropertyList without any buttons.
            /// </summary>
            public const string PropertyListEdit = "PropertyList_Edit";
        }

        public ModuleDefinition() {
            Temporary = true;
            if (IsModuleUnique)
                ModuleGuid = PermanentGuid;
            else
                ModuleGuid = Guid.NewGuid();

            //Displayed = true;
            //ActionLinkStyle = Actions.ActionLinkStyleEnum.SiteDefault;
            //PageAuthorization = true;
            //Secure = false;
            Visible = true;
            ModuleSecurity = PageDefinition.PageSecurityType.Any;
            ShowTitle = true;
            ShowTitleActions = true;
            ShowHelp = false;
            Name = __ResStr("name", "(unnamed)");
            Title = __ResStr("title", "(untitled)");
            Description = __ResStr("desc", "(not specified)");
            WantFocus = true;
            WantSearch = true;
            DateCreated = DateUpdated = DateTime.UtcNow;
            ShowFormButtons = true;
            Print = true;
            ReferencedModules = new SerializableList<ReferencedModule>();
            DefaultViewName = null;
            PopupPage = SkinAccess.POPUP_VIEW_DEFAULT;
            ModuleSkin = SkinAccess.MODULE_SKIN_DEFAULT;
        }

        [Data_DontSave]
        public string? DefaultViewName { get; set; }

        [JsonIgnore] // so it's not saved when json serializing site properties
        public virtual List<string> CategoryOrder { get { return new List<string> { "General", "Authorization", "Skin", "References", "Rss", "About", "Variables" }; } }

        [Category("Variables"), Caption("Permanent Guid"), Description("Displays a unique identifier for this type of module. This is typically used for development purposes only and can be used to uniquely identify this module type. This id never changes")]
        [UIHint("Guid"), ReadOnly]
        public Guid PermanentGuid {
            get {
                return GetPermanentGuid(GetType());
            }
        }
        [Category("General"), Caption("Name"), Description("The module name, which is used to identify the module", Order = -101)]
        [UIHint("Text40"), StringLength(ModuleDefinition.MaxName), Required, Trim]
        public string Name { get; set; }

        [Category("General"), Caption("Title"), Description("The module title, which appears at the top of the module as its title", Order = -100)]
        [UIHint("MultiString80"), StringLength(ModuleDefinition.MaxTitle), Trim]
        public MultiString Title { get; set; }

        [Category("General"), Caption("Visible"), Description("Defines whether the module is visible", Order = -98)]
        [UIHint("Boolean")]
        public bool Visible { get; set; }

        [Category("General"), Caption("Security"), Description("Defines what page security is needed for the module to be shown", Order = -96)]
        [UIHint("Enum")]
        public PageDefinition.PageSecurityType ModuleSecurity { get; set; }

        [Category("General"), Caption("Search Keywords"), Description("Defines whether this module's contents should be added to the site's search keywords", Order = -93)]
        [UIHint("Boolean")]
        public bool WantSearch { get; set; }

        [Category("General"), Caption("Wants Input Focus"), Description("Defines whether input fields in this module should receive the input focus if it's first on the page", Order = -91)]
        [UIHint("Boolean")]
        public bool WantFocus { get; set; }

        public static Guid GetPermanentGuid(Type moduleType) {
            ModuleGuidAttribute? attr = (ModuleGuidAttribute?) Attribute.GetCustomAttribute(moduleType, typeof(ModuleGuidAttribute));
            if (attr == null)
                throw new InternalError($"No {nameof(ModuleGuidAttribute)} in {moduleType.FullName}");
            return attr.Value;
        }
        public static string GetModulePermanentUrl(Type type) {
            return Globals.ModuleUrl + GetModuleGuidName(GetPermanentGuid(type));
        }
        public static string GetModulePermanentUrl(Guid permanentGuid) {
            return Globals.ModuleUrl + GetModuleGuidName(permanentGuid);
        }
        public static string GetModuleUrl(Guid guid) {
            return Globals.ModuleUrl + GetModuleGuidName(guid);
        }
        [Category("Variables"), Caption("Permanent Url"), Description("The Url used to uniquely and permanently identify this module")]
        [UIHint("Url"), AdditionalMetadata("UrlType", UrlTypeEnum.Local), ReadOnly]
        public string ModulePermanentUrl {
            get {
                return GetModulePermanentUrl(GetType());
            }
        }

        [Category("About"), Caption("Summary"), Description("The module description", Order = -100)]
        [UIHint("MultiString80"), StringLength(MaxDescription), ReadOnly]
        [Data_DontSave]
        public MultiString Description { get; set; }

        [Category("Variables"), Caption("Module Guid"), Description("Displays a unique identifier for this instance of a module. This is typically used for development purposes only and can be used to uniquely identify this module. This id never changes, even if the module is later renamed", Order = -100)]
        [UIHint("Guid"), ReadOnly, CopyAttribute]
        [Data_PrimaryKey]
        public Guid ModuleGuid { get; set; }

        /// <summary>
        /// Returns the module's unique id as a string.
        /// </summary>
        public string ModuleGuidName {
            get {
                return GetModuleGuidName(ModuleGuid);
            }
        }
        protected static string GetModuleGuidName(Guid moduleGuid) {
            return moduleGuid.ToString();
        }

        [Category("Variables"), Caption("Date Created"), Description("The date/time this module was created", Order = -96)]
        [UIHint("DateTime"), ReadOnly]
        public DateTime DateCreated { get; set; }
        [Category("Variables"), Caption("Date Updated"), Description("The date/time this module was last updated", Order = -95)]
        [UIHint("DateTime"), ReadOnly]
        public DateTime DateUpdated { get; set; }

        /// <summary>
        /// Module Id used in Html
        /// </summary>
        /// <remarks>
        /// The module Id can change between different requests and is only valid while
        /// the module definition is instantiated.
        /// </remarks>
        //[Category("Variables"), Caption("Module Html Id"), Description("The id used by this module in HTML. The module id can change between page requests and is not usable across http requests", Order = -94)]
        //[UIHint("String")
        public virtual string ModuleHtmlId {
            get {
                if (string.IsNullOrEmpty(_moduleHtmlId))
                    _moduleHtmlId = Manager.UniqueId("Mod");
                return _moduleHtmlId;
            }
        }
        private string? _moduleHtmlId;

        [Category("Variables"), Caption("Temporary"), Description("Defines whether the module is a temporary (generated) module", Order = -92)]
        [UIHint("Boolean"), ReadOnly, DontSave]
        public bool Temporary { get; set; }

        // SKIN
        // SKIN
        // SKIN

        [Category("Skin"), Caption("Module Skin"), Description("The module skin used for the module")]
        [UIHint("SkinNameModule"), StringLength(SiteDefinition.MaxPopupPage)]
        [Data_NewValue]
        public string? ModuleSkin { get; set; }
        public string ModuleSkin_Collection { get { return Manager.CurrentSite.Skin.Collection; } }

        [Category("Skin"), Caption("Popup Page"), Description("The popup page used for the popup window when this module is shown in a popup")]
        [UIHint("SkinNamePopup"), AdditionalMetadata("NoDefault", false), StringLength(SiteDefinition.MaxPopupPage)]
        [Data_NewValue]
        public string? PopupPage { get; set; }
        public string PopupPage_Collection { get { return Manager.CurrentSite.Skin.Collection; } }

        /// <summary>
        /// The CSS class name used on the &lt;div&gt; tag for this module. The allowable CSS class name is a subset of the CSS specification. Only characters _, a-z, A-Z and 0-9 are allowed, Ansi and Unicode escapes are not allowed.
        /// </summary>
        [Category("Skin"), Caption("CSS Class"), Description("The optional CSS classes to be added to the module's <div> tag for further customization through stylesheets")]
        [UIHint("Text40"), StringLength(MaxCssClass), CssClassesValidationAttribute, Trim]
        public string? CssClass { get; set; }

        /// <summary>
        /// The CSS class name to add to a temporary page's &lt;body&gt; tag when this module is used on a temporary page. Temporary pages are used when a module is displayed without a permanent, designed page.
        /// </summary>
        [Category("Skin"), Caption("Temp. Page CSS Class"), Description("The optional CSS classes to be added to a temporary page's <body> tag when this module is used on a temporary page - Temporary pages are used when a module is displayed without a permanent, designed page")]
        [UIHint("Text40"), StringLength(MaxCssClass), CssClassesValidationAttribute, Trim]
        public string? TempPageCssClass { get; set; }

        /// <summary>
        /// Defines whether the skin's partial form CSS is added to partial forms.
        /// </summary>
        [Category("Skin"), Caption("Partial Form CSS"), Description("Defines whether the skin's partial form CSS is added to partial forms within this module - Partial form CSS is never used in popup windows or on mobile devices")]
        [UIHint("Boolean")]
        [Data_DontSave]
        public bool UsePartialFormCss { get { return !_SuppressPartialFormCss; } set { _SuppressPartialFormCss = !value; } }
        [Data_NewValue]
        [NoModelChange]
        public bool _SuppressPartialFormCss { get; set; }


        [Category("Skin"), Caption("Show Title"), Description("Defines whether the module title is shown - Applies to the modStandard skin only")]
        [UIHint("Boolean")]
        [ProcessIf(nameof(ModuleSkin), SkinAccess.MODULE_SKIN_DEFAULT, Disable = true)]
        public bool ShowTitle { get; set; }

        [Category("Skin"), Caption("Show Actions (Title)"), Description("Defines whether the module's action links are also shown next to the module title - Only the icons are shown if selected - Applies to the modStandard skin only")]
        [UIHint("Boolean")]
        [ProcessIf(nameof(ModuleSkin), SkinAccess.MODULE_SKIN_DEFAULT, nameof(ShowTitle), true, Disable = true)]
        [Data_NewValue]
        public bool ShowTitleActions { get; set; }

        [Category("Skin"), Caption("Can Minimize"), Description("Defines whether the module can be minimized - Applies to the modPanel skin only")]
        [UIHint("Boolean")]
        [ProcessIf(nameof(ModuleSkin), SkinAccess.MODULE_SKIN_PANEL, Disable = true)]
        [Data_NewValue]
        public bool CanMinimize { get; set; }

        [Category("Skin"), Caption("Start Minimized"), Description("Defines whether the module is initially minimized - Applies to the modPanel skin only")]
        [UIHint("Boolean")]
        [ProcessIf(nameof(ModuleSkin), SkinAccess.MODULE_SKIN_PANEL, Disable = true)]
        [Data_NewValue]
        public bool Minimized { get; set; }

        [Category("Skin"), Caption("Show Help"), Description("Defines whether the module help link is shown in Display Mode - The help link is always shown in Edit Mode")]
        [UIHint("Boolean")]
        public bool ShowHelp { get; set; }

        [Category("Skin"), Caption("Help URL"), Description("Defines the URL used to display help for this module - If omitted, the package's help link is used instead")]
        [UIHint("Url"), AdditionalMetadata("UrlType", UrlTypeEnum.Local | UrlTypeEnum.Remote), UrlValidation(UrlValidationAttribute.SchemaEnum.Any, UrlTypeEnum.Local | UrlTypeEnum.Remote)]
        [StringLength(Globals.MaxUrl), Trim]
        [Data_NewValue]
        public string? HelpURL { get; set; }

        [Category("Skin"), Caption("Print Support"), Description("Defines whether the module is printed when a page is printed")]
        [UIHint("Boolean")]
        public bool Print { get; set; }

        [Category("Skin"), Caption("Show Form Buttons"), Description("If the module has a form with buttons (Save, Close, Return, etc.) these are shown/hidden based on this setting")]
        [UIHint("Boolean")]
        public bool ShowFormButtons { get; set; }

        [Category("Skin"), Caption("Anchor Id"), Description("The optional id used as anchor tag for this module - if an id is entered, an anchor tag is generated so the module can be directly located on the page")]
        [UIHint("Text40"), StringLength(MaxHtmlId), AnchorValidationAttribute, Trim]
        public virtual string? AnchorId { get; set; }

        [Category("Skin"), Caption("AutoComplete"), Description("Defines whether autocomplete is used for input fields on the form")]
        [UIHint("Boolean")]
        [Data_NewValue]
        public bool FormAutoComplete { get; set; }

        // AUTHORIZATION
        // AUTHORIZATION
        // AUTHORIZATION

        [Data_Binary]
        [Category("Authorization"), Caption("Permitted Roles"), Description("Roles that are permitted to use this module")]
        [UIHint("YetaWF_ModuleEdit_AllowedRoles")]
        public SerializableList<AllowedRole> AllowedRoles {
            get {
                if (_allowedRoles == null)
                    _allowedRoles = DefaultAllowedRoles;
                return _allowedRoles;
            }
            set {
                _allowedRoles = value;
            }
        }
        private SerializableList<AllowedRole>? _allowedRoles;

        [Data_Binary]
        [Category("Authorization"), Caption("Permitted Users"), Description("Users that are permitted to use this module")]
        [UIHint("YetaWF_ModuleEdit_AllowedUsers")]
        public SerializableList<AllowedUser> AllowedUsers {
            get {
                if (_allowedUsers == null)
                    _allowedUsers = new SerializableList<AllowedUser>();
                return _allowedUsers;
            }
            set {
                _allowedUsers = value;
            }
        }
        private SerializableList<AllowedUser>? _allowedUsers;

        /// <summary>
        /// Returns the module's default allowed roles
        /// </summary>
        public virtual SerializableList<AllowedRole>  DefaultAllowedRoles { get { return UserLevel_DefaultAllowedRoles; } }

        /// <summary>
        /// Returns the anonymous role and all others as allowed roles
        /// </summary>
        public SerializableList<AllowedRole> AnonymousLevel_DefaultAllowedRoles {
            get {
                return new SerializableList<AllowedRole>() {
                    new AllowedRole(Resource.ResourceAccess.GetAnonymousRoleId(), AllowedEnum.Yes),
                    new AllowedRole(Resource.ResourceAccess.GetUserRoleId(), AllowedEnum.Yes),
                    new AllowedRole(Resource.ResourceAccess.GetEditorRoleId(), AllowedEnum.Yes, AllowedEnum.Yes),
                    new AllowedRole(Resource.ResourceAccess.GetAdministratorRoleId(), AllowedEnum.Yes, AllowedEnum.Yes, AllowedEnum.Yes, AllowedEnum.Yes, AllowedEnum.Yes, AllowedEnum.Yes, AllowedEnum.Yes, AllowedEnum.Yes),
                };
            }
        }

        /// <summary>
        /// Returns the default allowed roles - user, editor and admin
        /// </summary>
        public SerializableList<AllowedRole> UserLevel_DefaultAllowedRoles {
            get {
                return new SerializableList<AllowedRole>() {
                    new AllowedRole(Resource.ResourceAccess.GetUserRoleId(), AllowedEnum.Yes),
                    new AllowedRole(Resource.ResourceAccess.GetEditorRoleId(), AllowedEnum.Yes, AllowedEnum.Yes),
                    new AllowedRole(Resource.ResourceAccess.GetAdministratorRoleId(), AllowedEnum.Yes, AllowedEnum.Yes, AllowedEnum.Yes, AllowedEnum.Yes, AllowedEnum.Yes, AllowedEnum.Yes, AllowedEnum.Yes, AllowedEnum.Yes),
                };
            }
        }

        /// <summary>
        /// Returns the admin role as allowed role
        /// </summary>
        public SerializableList<AllowedRole> EditorLevel_DefaultAllowedRoles {
            get {
                return new SerializableList<AllowedRole>() {
                    new AllowedRole(Resource.ResourceAccess.GetEditorRoleId(), AllowedEnum.Yes, AllowedEnum.Yes),
                    new AllowedRole(Resource.ResourceAccess.GetAdministratorRoleId(), AllowedEnum.Yes, AllowedEnum.Yes, AllowedEnum.Yes, AllowedEnum.Yes, AllowedEnum.Yes, AllowedEnum.Yes, AllowedEnum.Yes, AllowedEnum.Yes),
                };
            }
        }

        /// <summary>
        /// Returns the admin role as allowed role
        /// </summary>
        public SerializableList<AllowedRole> AdministratorLevel_DefaultAllowedRoles {
            get {
                return new SerializableList<AllowedRole>() {
                    new AllowedRole(Resource.ResourceAccess.GetAdministratorRoleId(), AllowedEnum.Yes, AllowedEnum.Yes, AllowedEnum.Yes, AllowedEnum.Yes, AllowedEnum.Yes, AllowedEnum.Yes, AllowedEnum.Yes, AllowedEnum.Yes),
                };
            }
        }

        /// <summary>
        /// Returns the superuser role as allowed role
        /// </summary>
        public SerializableList<AllowedRole> SuperuserLevel_DefaultAllowedRoles {
            get {
                return new SerializableList<AllowedRole>() {};
            }
        }

        public virtual List<RoleDefinition> ExtraRoles {
            get {
                return new List<RoleDefinition>();
            }
        }
        public List<RoleDefinition> RolesDefinitions {
            get {
                if (_rolesDefinitions == null) {
                    _rolesDefinitions = new List<RoleDefinition>() {
                        new RoleDefinition(RoleDefinition.View, __ResStr("roleViewC", "View"), __ResStr("roleView", "The role has permission to view the module"), __ResStr("userViewC", "View"), __ResStr("userView", "The user has permission to view the module")),
                        new RoleDefinition(RoleDefinition.Edit, __ResStr("roleEditC", "Edit"), __ResStr("roleEdit", "The role has permission to edit the module"), __ResStr("userEditC", "Edit"), __ResStr("userEdit", "The user has permission to edit the module")),
                        new RoleDefinition(RoleDefinition.Remove,  __ResStr("roleRemoveC", "Remove"), __ResStr("roleRemove", "The role has permission to remove the module"), __ResStr("userRemoveC", "Remove"), __ResStr("userRemove", "The user has permission to remove the module")),
                    };
                    _rolesDefinitions.AddRange(ExtraRoles);
                    int index = 0;
                    foreach (RoleDefinition roleDef in _rolesDefinitions)
                        roleDef.Index = index++;
                }
                return _rolesDefinitions;
            }
        }
        private List<RoleDefinition>? _rolesDefinitions;

        // defines external permission levels so they can be translated to internal levels (extra1,2...)
        public class RoleDefinition {

            public const string View = "View";
            public const string Edit = "Edit";
            public const string Remove = "Remove";

            public class Resource {
                public string Caption { get; set; } = null!;
                public string Description { get; set; } = null!;
            }
            public RoleDefinition(string name, string roleCaption, string roleDescription, string userCaption, string userDescription) {
                RoleResource = new RoleDefinition.Resource();
                UserResource = new RoleDefinition.Resource();
                Name = name;
                RoleResource.Caption = roleCaption;
                RoleResource.Description = roleDescription;
                UserResource.Caption = userCaption;
                UserResource.Description = userDescription;
            }
            public string Name { get; private set; }
            public int Index { get; set; }
            public Resource RoleResource { get; set; }
            public Resource UserResource { get; set; }
            public string InternalName {
                get {
                    if (Index == 0) return View;
                    else if (Index == 1) return Edit;
                    else if (Index == 2) return Remove;
                    else return "Extra" + (Index - 2).ToString();
                }
            }
        }

        // one grid entry used to edit a role
        public class GridAllowedRole {

            [Caption("Role"), Description("Role Description", Order = -100)]
            [UIHint("StringTT"), ReadOnly]
            public StringTT RoleName { get; set; } = null!;

            [Caption("View"), ResourceRedirectList(nameof(RolesDefinitions), 0, nameof(RoleDefinition.RoleResource)), Description("The role has permission to view the module", Order = -99)]
            [UIHint("Enum")]
            public AllowedEnum View { get; set; }

            [Caption("Edit"), ResourceRedirectList(nameof(RolesDefinitions), 1, nameof(RoleDefinition.RoleResource)), Description("The role has permission to edit the module", Order = -98)]
            [UIHint("Enum")]
            public AllowedEnum Edit { get; set; }

            [Caption("Remove"), ResourceRedirectList(nameof(RolesDefinitions), 2, nameof(RoleDefinition.RoleResource)), Description("The role has permission to remove the module", Order = -97)]
            [UIHint("Enum")]
            public AllowedEnum Remove { get; set; }

            [ResourceRedirectList(nameof(RolesDefinitions), 3, nameof(RoleDefinition.RoleResource))]
            [UIHint("Enum")]
            public virtual AllowedEnum Extra1 { get; set; }
            [ResourceRedirectList(nameof(RolesDefinitions), 4, nameof(RoleDefinition.RoleResource))]
            [UIHint("Enum")]
            public virtual AllowedEnum Extra2 { get; set; }
            [ResourceRedirectList(nameof(RolesDefinitions), 5, nameof(RoleDefinition.RoleResource))]
            [UIHint("Enum")]
            public virtual AllowedEnum Extra3 { get; set; }
            [ResourceRedirectList(nameof(RolesDefinitions), 6, nameof(RoleDefinition.RoleResource))]
            [UIHint("Enum")]
            public virtual AllowedEnum Extra4 { get; set; }
            [ResourceRedirectList(nameof(RolesDefinitions), 7, nameof(RoleDefinition.RoleResource))]
            [UIHint("Enum")]
            public virtual AllowedEnum Extra5 { get; set; }

            [UIHint("Hidden")]
            public int RoleId { get; set; }

            public bool __editable { get { return RoleId != Resource.ResourceAccess.GetSuperuserRoleId(); } }
        }

        public class GridAllowedUser {

            [Caption("Delete"), Description("Click to delete a user", Order = -100)]
            [UIHint("GridDeleteEntry"), ReadOnly]
            public int Delete { get; set; }

            [Caption("User"), Description("User Name", Order = -99)]
            [UIHint("YetaWF_Identity_UserId"), ReadOnly]
            public int DisplayUserId { get; set; }

            [Caption("View"), ResourceRedirectList(nameof(RolesDefinitions), 0, nameof(RoleDefinition.UserResource)), Description("The user has permission to view the module", Order = -98)]
            [UIHint("Enum")]
            public AllowedEnum View { get; set; }

            [Caption("Edit"), ResourceRedirectList(nameof(RolesDefinitions), 1, nameof(RoleDefinition.UserResource)), Description("The user has permission to edit the module", Order = -97)]
            [UIHint("Enum")]
            public AllowedEnum Edit { get; set; }

            [Caption("Remove"), ResourceRedirectList(nameof(RolesDefinitions), 2, nameof(RoleDefinition.UserResource)), Description("The user has permission to remove the module", Order = -96)]
            [UIHint("Enum")]
            public AllowedEnum Remove { get; set; }

            [ResourceRedirectList(nameof(RolesDefinitions), 3, nameof(RoleDefinition.UserResource))]
            [UIHint("Enum")]
            public virtual AllowedEnum Extra1 { get; set; }
            [ResourceRedirectList(nameof(RolesDefinitions), 4, nameof(RoleDefinition.UserResource))]
            [UIHint("Enum")]
            public virtual AllowedEnum Extra2 { get; set; }
            [ResourceRedirectList(nameof(RolesDefinitions), 5, nameof(RoleDefinition.UserResource))]
            [UIHint("Enum")]
            public virtual AllowedEnum Extra3 { get; set; }
            [ResourceRedirectList(nameof(RolesDefinitions), 6, nameof(RoleDefinition.UserResource))]
            [UIHint("Enum")]
            public virtual AllowedEnum Extra4 { get; set; }
            [ResourceRedirectList(nameof(RolesDefinitions), 7, nameof(RoleDefinition.UserResource))]
            [UIHint("Enum")]
            public virtual AllowedEnum Extra5 { get; set; }

            [UIHint("Hidden"), ReadOnly]
            public int UserId { get; set; }
            [UIHint("Hidden"), ReadOnly]
            public string DisplayUserName { get; set; } = null!;

            public async Task SetUserAsync(int userId) {
                DisplayUserId = UserId = userId;
                View = AllowedEnum.Yes;
                DisplayUserName = await Resource.ResourceAccess.GetUserNameAsync(userId);
            }
            public GridAllowedUser() { }
        }

        // SKINMODULE
        // SKINMODULE
        // SKINMODULE

        [Category("Variables"), Caption("Invokable"), Description("Defines whether this module can be referenced by other modules or pages so it's injected into the page - Typically used for skin modules - Only unique modules support being invoked")]
        [UIHint("Boolean"), ReadOnly]
        [DontSave]
        public bool Invokable { get; set; }

        /// <summary>
        /// Defines the class that causes this module to be injected at the end of the page.
        /// </summary>
        /// <remarks>Certain controls/templates use CSS that can be handled by skin modules. By defining InvokingCss, a module will automatically be
        /// injected to implement the control/template - typically this is a JavaScript/client side implementation.</remarks>
        [Category("Variables"), Caption("Invoking CSS"), Description("Defines the CSS that causes this module to be injected into the page, when the CSS is used by a template - only supported for unique modules")]
        [UIHint("String"), ReadOnly]
        [DontSave]
        public string? InvokingCss { get; protected set; }

        [Category("Variables"), Caption("Use In Popup"), Description("Defines whether this module can be injected in a popup")]
        [UIHint("Boolean")]
        [DontSave, ReadOnly]
        public bool InvokeInPopup { get; protected set; }

        [Category("Variables"), Caption("Use In Ajax"), Description("Defines whether this module can be injected in an Ajax request/partial view")]
        [DontSave, ReadOnly]
        [UIHint("Boolean")]
        public bool InvokeInAjax { get; protected set; }

        public class ReferencedModule {
            public Guid ModuleGuid { get; set; }
            public static void AddReferencedModule(SerializableList<ReferencedModule> refmods, Guid refGuid) {
                if (! (from r in refmods where r.ModuleGuid == refGuid select r).Any())
                    refmods.Add(new ModuleDefinition.ReferencedModule { ModuleGuid = refGuid });
            }
            public static void AddReferences(SerializableList<ModuleDefinition.ReferencedModule> referencedModules, SerializableList<ModuleDefinition.ReferencedModule> refMods) {
                foreach (ModuleDefinition.ReferencedModule refMod in refMods)
                    AddReferencedModule(referencedModules, refMod.ModuleGuid);
            }
        }

        /// <summary>
        /// Templates supported by this module (needed for ajax requests so pages can include all required template support even if the actual template isn't used until an ajax request occurs)
        /// </summary>
        [DontSave]
        public List<string>? SupportedTemplates { get; set; }

        [Category("References"), Caption("Other Modules"), Description("Defines other modules which must be injected into the page when this module is used")]
        [UIHint("ReferencedModules")]
        [Data_Binary]
        public SerializableList<ReferencedModule> ReferencedModules { get; set; }
    }
}
