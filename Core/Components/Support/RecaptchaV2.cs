﻿/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Threading.Tasks;
using YetaWF.Core.DataProvider.Attributes;
using YetaWF.Core.Models.Attributes;

namespace YetaWF.Core.Components {

    public class RecaptchaV2Data {
        public RecaptchaV2Data() { VerifyPresence = true; }
        [UIHint("Hidden")]
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
        public string PublicKey { get; set; } = null!;
        [StringLength(MaxPrivateKey)]
        public string PrivateKey { get; set; } = null!;

        public ThemeEnum Theme { get; set; }
        public SizeEnum Size { get; set; }

        public RecaptchaV2Config() {
            Theme = ThemeEnum.Light;
        }

        // LOAD/SAVE
        // LOAD/SAVE
        // LOAD/SAVE

        public string GetTheme() {
            return Theme switch {
                ThemeEnum.Dark => "dark",
                _ => "light",
            };
        }
        public string GetSize() {
            return Size switch {
                SizeEnum.Compact => "compact",
                _ => "normal",
            };
        }

        // These must be provided during app startup
        public static Func<Task<RecaptchaV2Config>> LoadRecaptchaV2ConfigAsync { get; set; } = null!;
        public static Func<RecaptchaV2Config, Task> SaveRecaptchaV2ConfigAsync { get; set; } = null!;
    }
}
