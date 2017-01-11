/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Web.Mvc;
using YetaWF.Core.Addons;
using YetaWF.Core.Models;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Views.Shared {

    public class MultiString<TModel> : RazorTemplate<TModel> { }

    public static class MultiStringHelper {

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        public static MvcHtmlString RenderMultiStringEdit(this HtmlHelper<object> htmlHelper, string name, MultiString ms, string divId, string cls, object HtmlAttributes = null) {

            string fullName = htmlHelper.FieldName(name);

            HtmlBuilder hb = new HtmlBuilder();

            // <div class="yt_multistring t_edit" data-name="@Html.FieldName("")" id="@DivId" class="@Html.GetErrorClass("")" ...validation...>
            TagBuilder tagDiv = new TagBuilder("div");
            tagDiv.AddCssClass(Manager.AddOnManager.CheckInvokedCssModule("yt_multistring"));
            tagDiv.AddCssClass("t_edit");
            tagDiv.AddCssClass("y_inline");
            tagDiv.Attributes["data-name"] = fullName;
            tagDiv.Attributes["id"] = divId;

            // use hidden input fields for each language available
            int counter = 0;
            foreach (var lang in MultiString.Languages) {
                TagBuilder tag = new TagBuilder("input");
                tag.MergeAttribute("type", "hidden");
                string n = string.Format("{0}[{1}].key", fullName, counter);
                tag.MergeAttribute("name", n, true);
                tag.MergeAttribute("value", lang.Id);
                hb.Append(tag.ToString(TagRenderMode.StartTag));

                tag = new TagBuilder("input");
                tag.MergeAttribute("type", "hidden");
                n = string.Format("{0}[{1}].value", fullName, counter);
                tag.MergeAttribute("name", n, true);
                tag.MergeAttribute("value", ms[lang.Id]);
                hb.Append(tag.ToString(TagRenderMode.StartTag));

                ++counter;
            }

            // determine which language to select by default (Active or Default)
            // the active language can only be selected if the default language text is available
            string selectLang = MultiString.ActiveLanguage;
            if (String.IsNullOrWhiteSpace(ms[MultiString.DefaultLanguage]))
                selectLang = MultiString.DefaultLanguage;

            // generate a textbox for the currently selected language
            hb.Append(htmlHelper.RenderTextBox("", ms[selectLang], HtmlAttributes: new { @class = cls + " yt_multistring_text yt_text_base " + Forms.CssFormNoSubmit }));

            // generate a dropdownlist for the available languages
            List<SelectionItem<string>> selectLangList = new List<SelectionItem<string>>();
            foreach (var lang in MultiString.Languages) {
                selectLangList.Add(new SelectionItem<string> { Text = lang.ShortName, Value = lang.Id, Tooltip = lang.Description });
            }
            string idDD = Manager.UniqueId("lng");
            hb.Append(htmlHelper.RenderDropDownSelectionList<string>(
                "Language",
                selectLang,
                selectLangList,
                HtmlAttributes:
                    Manager.CurrentSite.Localization ?
                        (object) new {
                            Id = idDD,
                            @class = Forms.CssFormNoSubmit
                        } :
                        (object) new {
                            disabled = "disabled", Id = idDD,
                            @class = Forms.CssFormNoSubmit
                        }
            ));
            tagDiv.InnerHtml = hb.ToString();
            return MvcHtmlString.Create(tagDiv.ToString());
        }
    }
}
