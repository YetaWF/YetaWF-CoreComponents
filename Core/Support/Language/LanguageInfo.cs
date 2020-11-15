/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

#nullable enable

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
        public string Id { get; set; } = null!;
        [StringLength(MaxShortName)]
        public string ShortName { get; set; } = null!;
        [StringLength(MaxDescription)]
        public string Description { get; set; } = null!;

        public LanguageData() { }
    }

    public static class LanguageInfo {

        public static ILanguages LanguagesAccess { get; set; } = null!;

    }
}
