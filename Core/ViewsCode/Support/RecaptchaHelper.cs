﻿/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using YetaWF.Core.DataProvider.Attributes;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;
#if MVC6
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
#else
using System.Web;
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Views.Shared {

    public class RecaptchaData {
        public RecaptchaData() { }
        public bool VerifyPresence { get; set; }
    }
    public class RecaptchaConfig {

        public const int MaxPublicKey = 100;
        public const int MaxPrivateKey = 100;

        public enum ThemeEnum {
            [EnumDescription("Red")]
            Red = 0,
            [EnumDescription("White")]
            White = 1,
            [EnumDescription("Black")]
            Blackglass = 2,
            [EnumDescription("Clean")]
            Clean = 3,
        }

        [Data_PrimaryKey]
        public int Key { get; set; }

        [StringLength(MaxPublicKey)]
        public string PublicKey { get; set; }
        [StringLength(MaxPrivateKey)]
        public string PrivateKey { get; set; }

        public ThemeEnum Theme { get; set; }

        public RecaptchaConfig() {
            Theme = ThemeEnum.White;
        }

        // LOAD/SAVE
        // LOAD/SAVE
        // LOAD/SAVE

        public string GetTheme() {
            switch (Theme) {
                default:
                case ThemeEnum.White: return "white";
                case ThemeEnum.Red: return "red";
                case ThemeEnum.Blackglass: return "blackglass";
                case ThemeEnum.Clean: return "clean";
            }
        }

        // These must be provided during app startup
        public static Func<RecaptchaConfig> LoadRecaptchaConfig { get; set; }
        public static Action<RecaptchaConfig> SaveRecaptchaConfig { get; set; }
    }

    public class Recaptcha<TModel> : RazorTemplate<TModel> { }

    public static class RecaptchaHelper {
#if MVC6
        public static HtmlString RenderRecaptcha(this IHtmlHelper htmlHelper, string name, RecaptchaData value, int dummy = 0, object HtmlAttributes = null) {
#else
        public static HtmlString RenderRecaptcha(this HtmlHelper<object> htmlHelper, string name, RecaptchaData value, int dummy = 0, object HtmlAttributes = null) {
#endif
            HtmlBuilder hb = new HtmlBuilder();
            hb.Append(htmlHelper.RenderHidden("VerifyPresence", value.VerifyPresence));

            TagBuilder tag = new TagBuilder("div");
            htmlHelper.FieldSetup(tag, name, HtmlAttributes: HtmlAttributes, Validation: false, Anonymous: true);
            hb.Append(tag.ToString(TagRenderMode.Normal));

            return hb.ToHtmlString();
        }
    }
}
