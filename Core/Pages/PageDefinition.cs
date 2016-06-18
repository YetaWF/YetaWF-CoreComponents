/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Linq;
using YetaWF.Core.DataProvider.Attributes;
using YetaWF.Core.Language;
using YetaWF.Core.Models;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Modules;
using YetaWF.Core.Serializers;
using YetaWF.Core.Skins;

namespace YetaWF.Core.Pages {
    [Trim]
    public partial class PageDefinition
    {
        public const int MaxPane = 40;
        public const int MaxTitle = 100;
        public const int MaxDescription = 200;
        public const int MaxKeywords = 100;
        public const int MaxCopyright = 100;
        public const int MaxjQueryUISkin = 100;
        public const int MaxKendoUISkin = 100;
        public const int MaxSyntaxHighlighterSkin = 100;
        public const int MaxCssClass = 40;

        public enum PageSecurityType {
            [EnumDescription("Always Show")]
            Any = 0,
            [EnumDescription("Show With https:// Only")]
            httpsOnly = 1,
            [EnumDescription("Show With http:// Only")]
            httpOnly = 2,
        }

        public PageDefinition() {
            Temporary = true;
            PageGuid = Guid.NewGuid();
            SelectedSkin = new SkinDefinition {
                Collection = null,
                FileName = SkinAccess.FallbackPageFileName,
            };
            SelectedPopupSkin = new SkinDefinition {
                Collection = null,
                FileName = SkinAccess.FallbackPopupFileName,
            };
            jQueryUISkin = null;
            KendoUISkin = null;
            SyntaxHighlighterSkin = null;
            Title = new MultiString();
            Description = new MultiString();
            Keywords = new MultiString();
            ModuleDefinitions = new ModuleList();
            PageSecurity = PageSecurityType.Any;
            Url = null;
            AllowedRoles = new SerializableList<AllowedRole>();
            AllowedUsers = new SerializableList<AllowedUser>();
            Copyright = new MultiString();
            Created = Updated = DateTime.UtcNow;
            WantSearch = true;
            FavIcon_Data = new byte[0];
            ReferencedModules = new SerializableList<ModuleDefinition.ReferencedModule>();
        }

        public enum AllowedEnum {
            [EnumDescription(" ", "Not specified")]
            NotDefined = 0,
            [EnumDescription("Yes", "Allowed")]
            Yes = 1,
            [EnumDescription("No", "Forbidden")]
            No = 2,
        };

        public class AllowedRole {
            public int RoleId { get; set; }
            public AllowedEnum View { get; set; }
            public AllowedEnum Edit { get; set; }
            public AllowedEnum Remove { get; set; }
            public bool IsEmpty() { return View == AllowedEnum.NotDefined && Edit == AllowedEnum.NotDefined && Remove == AllowedEnum.NotDefined; }
            public static AllowedRole Find(List<AllowedRole> list, int roleId) {
                if (list == null) return null;
                return (from l in list where roleId == l.RoleId select l).FirstOrDefault();
            }
        }
        public class AllowedUser {
            public int UserId { get; set; }
            public AllowedEnum View { get; set; }
            public AllowedEnum Edit { get; set; }
            public AllowedEnum Remove { get; set; }
            public bool IsEmpty() { return View == AllowedEnum.NotDefined && Edit == AllowedEnum.NotDefined && Remove == AllowedEnum.NotDefined; }
            public static AllowedUser Find(List<AllowedUser> list, int userId) {
                if (list == null) return null;
                return (from l in list where userId == l.UserId select l).FirstOrDefault();
            }
        }

        [Data_Identity] // needed for ModuleDefinitions which creates a subtable
        public int Identity { get; set; }

        // When adding new properties, make sure to update EditablePage in PageEditModule so we can actually edit the property
        // When adding new properties, make sure to update EditablePage in PageEditModule so we can actually edit the property
        // When adding new properties, make sure to update EditablePage in PageEditModule so we can actually edit the property

        /// <summary>
        /// The unique page id
        /// </summary>
        [Data_PrimaryKey]
        public Guid PageGuid { get; set; }

        public SkinDefinition SelectedSkin { get; set; }
        public SkinDefinition SelectedPopupSkin { get; set; }

        public Guid? TemplateGuid { get; set; }

        [StringLength(MaxCssClass)]
        public string CssClass { get; set; }

        [StringLength(MaxjQueryUISkin)]
        public string jQueryUISkin { get; set; }
        [StringLength(MaxKendoUISkin)]
        public string KendoUISkin { get; set; }
        [StringLength(MaxSyntaxHighlighterSkin)]
        public string SyntaxHighlighterSkin { get; set; }

        [StringLength(Globals.MaxUrl)]
        public string Url { get; set; }
        [StringLength(Globals.MaxUrl)]
        public string CanonicalUrl { get; set; }

        public string CompleteUrl { get { return (string.IsNullOrWhiteSpace(CanonicalUrl)) ? Url : CanonicalUrl; } }

        [StringLength(MaxTitle)]
        public MultiString Title { get; set; }
        [StringLength(MaxDescription)]
        public MultiString Description { get; set; }
        [StringLength(MaxKeywords)]
        public MultiString Keywords { get; set; }
        [StringLength(MaxCopyright)]
        public MultiString Copyright { get; set; }
        public bool WantSearch { get; set; }

        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }

        public PageSecurityType PageSecurity { get; set; }

        [StringLength(Globals.MaxUrl)]
        public string MobilePageUrl { get; set; }

        [StringLength(Globals.MaxUrl)]
        public string RedirectToPageUrl { get; set; }

        [Data_Binary]
        public SerializableList<AllowedRole> AllowedRoles { get; set; }
        [Data_Binary]
        public SerializableList<AllowedUser> AllowedUsers { get; set; }

        [DontSave, ReadOnly]
        public bool Temporary { get; set; }

        [UIHint("Image")]
        [DontSave]
        public string FavIcon {
            get {
                if (_favIcon == null) {
                    if (FavIcon_Data != null && FavIcon_Data.Length > 0)
                        _favIcon = PageGuid.ToString();
                }
                return _favIcon;
            }
            set {
                _favIcon = value;
            }
        }
        private string _favIcon = null;

        [Data_Binary, CopyAttribute]
        public byte[] FavIcon_Data { get; set; }

        [Data_Binary]
        public SerializableList<ModuleDefinition.ReferencedModule> ReferencedModules { get; set; }

        public bool RobotNoIndex { get; set; }
        public bool RobotNoFollow { get; set; }
        public bool RobotNoArchive { get; set; }
        public bool RobotNoSnippet { get; set; }

        [StringLength(LanguageData.MaxId)]
        public string LanguageId { get; set; }

        // When adding new properties, make sure to update EditablePage in PageEditModule so we can actually edit the property
        // When adding new properties, make sure to update EditablePage in PageEditModule so we can actually edit the property
        // When adding new properties, make sure to update EditablePage in PageEditModule so we can actually edit the property
    }
}
