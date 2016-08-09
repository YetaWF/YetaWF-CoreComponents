﻿/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using YetaWF.Core.DataProvider.Attributes;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Pages;

namespace YetaWF.Core.Views.Shared {

    public class RecaptchaV2Data {
        public RecaptchaV2Data() { VerifyPresence = true; }
        public bool VerifyPresence { get; set; }
    }
    public class RecaptchaV2Config {

        public const int MaxPublicKey = 100;
        public const int MaxPrivateKey = 100;

        public enum ThemeEnum {
            [EnumDescription("Light")]
            Light = 0,
            [EnumDescription("Dark")]
            Dark = 1,
        }
        public enum SizeEnum {
            [EnumDescription("Normal")]
            Normal = 0,
            [EnumDescription("Compact")]
            Compact = 1,
        }

        [Data_PrimaryKey]
        public int Key { get; set; }

        [StringLength(MaxPublicKey)]
        public string PublicKey { get; set; }
        [StringLength(MaxPrivateKey)]
        public string PrivateKey { get; set; }

        public ThemeEnum Theme { get; set; }
        public SizeEnum Size { get; set; }

        public RecaptchaV2Config() {
            Theme = ThemeEnum.Light;
        }

        // LOAD/SAVE
        // LOAD/SAVE
        // LOAD/SAVE

        public string GetTheme() {
            switch (Theme) {
                default:
                case ThemeEnum.Light: return "light";
                case ThemeEnum.Dark: return "dark";
            }
        }
        public string GetSize() {
            switch (Size) {
                default:
                case SizeEnum.Normal: return "normal";
                case SizeEnum.Compact: return "compact";
            }
        }

        // These must be provided during app startup
        public static Func<RecaptchaV2Config> LoadRecaptchaV2Config { get; set; }
        public static Action<RecaptchaV2Config> SaveRecaptchaV2Config { get; set; }
    }

    public class RecaptchaV2<TModel> : RazorTemplate<TModel> { }
}
