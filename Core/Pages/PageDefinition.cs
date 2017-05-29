/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Linq;
using YetaWF.Core.DataProvider.Attributes;
using YetaWF.Core.Language;
using YetaWF.Core.Models;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Modules;
using YetaWF.Core.Serializers;
using YetaWF.Core.Site;
using YetaWF.Core.Skins;
using YetaWF.Core.Support;

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
        public enum StaticPageEnum {
            [EnumDescription("No", "The page is always rendered with current data")]
            No = 0,
            [EnumDescription("Yes", "The page is a static page (internally saved as a file)")]
            Yes = 1,
            [EnumDescription("Yes, In Memory", "The page is a static page (internally saved in memory)")]
            YesMemory = 2,
        }
        public enum ChangeFrequencyEnum {
            [EnumDescription("(Site Default)", "Use the site defined default Change Frequency")]
            Default = 0,
            [EnumDescription("Always", "The page may change each time it is accessed")]
            Always = 1,
            [EnumDescription("Hourly", "The page may change hourly")]
            Hourly = 2,
            [EnumDescription("Daily", "The page may change daily")]
            Daily = 3,
            [EnumDescription("Weekly", "The page may change weekly")]
            Weekly = 4,
            [EnumDescription("Monthly", "The page may change monthly")]
            Monthly = 5,
            [EnumDescription("Yearly", "The page may change yearly")]
            Yearly = 6,
            [EnumDescription("Never", "The page never changes - use for archived pages")]
            Never = 7,
        }
        public enum ChangeFrequencySiteEnum {
            [EnumDescription("Always", "Pages may change each time they are accessed")]
            Always = 1,
            [EnumDescription("Hourly", "Pages may change hourly")]
            Hourly = 2,
            [EnumDescription("Daily", "Pages may change daily")]
            Daily = 3,
            [EnumDescription("Weekly", "Pages may change weekly")]
            Weekly = 4,
            [EnumDescription("Monthly", "Pages may change monthly")]
            Monthly = 5,
            [EnumDescription("Yearly", "Pages may change yearly")]
            Yearly = 6,
            [EnumDescription("Never", "Pages never change - use for archived pages")]
            Never = 7,
        }
        public enum SiteMapPriorityEnum {
            [EnumDescription("(Site Default)", "Use the site defined default page priority")]
            Default = 0,
            [EnumDescription("(Exclude From Site Map)", "The page is never added to the site map")]
            Excluded = -1,
            [EnumDescription("None")]
            None = 10,
            [EnumDescription("Really Low")]
            SuperLow = 20,
            [EnumDescription("Low")]
            Low = 30,
            [EnumDescription("Below Medium")]
            BelowMedium = 40,
            [EnumDescription("Medium")]
            Medium = 50,
            [EnumDescription("Above Medium")]
            AboveMedium = 70,
            [EnumDescription("High")]
            High = 90,
            [EnumDescription("Highest")]
            Top = 100,
        }
        public enum SiteMapPrioritySiteEnum {
            [EnumDescription("(Exclude From Site Map)", "Pages are never added to the site map")]
            Excluded = -1,
            [EnumDescription("None")]
            None = 10,
            [EnumDescription("Really Low")]
            SuperLow = 20,
            [EnumDescription("Low")]
            Low = 30,
            [EnumDescription("Below Medium")]
            BelowMedium = 40,
            [EnumDescription("Medium")]
            Medium = 50,
            [EnumDescription("Above Medium")]
            AboveMedium = 70,
            [EnumDescription("High")]
            High = 90,
            [EnumDescription("Highest")]
            Top = 100,
        }
        public enum UnifiedModeEnum {
            [EnumDescription("None", "The unified page set does not combine page content - Each page is shown individually (used to disable the unified page set)")]
            None = 0,
            [EnumDescription("Hide Others", "Only content for the current Url is shown - Content for other pages is embedded but not visible")]
            HideDivs = 1, // divs for other urls are hidden
            [EnumDescription("Show All Content", "All content is shown in the order the pages appear in the unified page set")]
            ShowDivs = 2, // all divs are shown
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
            ChangeFrequency = ChangeFrequencyEnum.Default;
            SiteMapPriority = SiteMapPriorityEnum.Medium;
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

        /// <summary>
        /// The page used as template for the current page.
        /// </summary>
        public Guid? TemplateGuid { get; set; }
        /// <summary>
        /// Defines the unified set of pages this page belongs to (if any).
        /// </summary>
        [Data_Index]
        public Guid? UnifiedSetGuid { get; set; }

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
        /// <summary>
        /// User-defined canonical Url.
        /// </summary>
        /// <remarks>The user-defined canonical Url may contain variables used for variable substitution.
        ///
        /// Modules that override the canonical Url should use EvaluatedCanonicalUrl instead.</remarks>
        [StringLength(Globals.MaxUrl)]
        public string CanonicalUrl { get; set; }

        /// <summary>
        /// Actual canonical Url.
        /// </summary>
        /// <remarks>Unlike CanonicalUrl, the EvaluatedCanonicalUrl substitutes all variables.</remarks>
        [Data_DontSave]
        public string EvaluatedCanonicalUrl {
            get {
                if (string.IsNullOrWhiteSpace(_canonicalUrl)) {
                    string url = string.IsNullOrWhiteSpace(CanonicalUrl) ? Url : CanonicalUrl;
                    Variables vars = new Variables(Manager);
                    url = vars.ReplaceVariables(url);
                    _canonicalUrl = Manager.CurrentSite.MakeUrl(YetaWFManager.UrlEncodePath(url), PagePageSecurity: PageSecurity);
                }
                return _canonicalUrl;
            }
            set {
                if (value != null) {
                    if (value.StartsWith("/")) {
                        _canonicalUrl = Manager.CurrentSite.MakeUrl(value);
                        return;
                    }
                }
                _canonicalUrl = value;
            }
        }
        private string _canonicalUrl;

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

        [Data_NewValue("(0)")]
        public StaticPageEnum StaticPage { get; set; }

        [StringLength(Globals.MaxUrl)]
        public string MobilePageUrl { get; set; }

        [StringLength(Globals.MaxUrl)]
        public string RedirectToPageUrl { get; set; }

        [StringLength(SiteDefinition.MaxAnalytics)]
        public string Analytics { get; set; }

        [StringLength(SiteDefinition.MaxMeta)]
        public string PageMetaTags { get; set; }

        [Data_NewValue("(0)")]
        public ChangeFrequencyEnum ChangeFrequency { get; set; }
        [Data_NewValue("(0)")]
        public SiteMapPriorityEnum SiteMapPriority { get; set; }

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
