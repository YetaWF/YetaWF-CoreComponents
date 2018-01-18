/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;
using YetaWF.Core.DataProvider.Attributes;
using YetaWF.Core.Models.Attributes;

namespace YetaWF.Core.Language {

    public interface ILanguages {
        List<LanguageData> GetAllLanguages();
    }

    public class LanguageData {

        public const int MaxId = 20;
        public const int MaxShortName = 40;
        public const int MaxDescription = 200;

        [Data_PrimaryKey, StringLength(MaxId)]
        public string Id { get; set; }
        [StringLength(MaxShortName)]
        public string ShortName { get; set; }
        [StringLength(MaxDescription)]
        public string Description { get; set; }

        public LanguageData() { }
    }

    public static class LanguageInfo {

        public static ILanguages LanguagesAccess { get; set; }

    }
}
