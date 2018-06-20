/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using YetaWF.Core.Addons;
using YetaWF.Core.Image;
using YetaWF.Core.Modules;
using YetaWF.Core.Support;
#if MVC6
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
#else
using System.Web.Mvc;
using System.Web.Mvc.Html;
#endif

namespace YetaWF.Core.Views {

    public static class RazorViewExtensions {

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }
#if MVC6
        internal static string PostProcessViewHtml(IHtmlHelper htmlHelper, ModuleDefinition module, string viewHtml, bool UsePartialFormCss = true) {
#else
        internal static string PostProcessViewHtml(HtmlHelper htmlHelper, ModuleDefinition module, string viewHtml, bool UsePartialFormCss = true) {
#endif
            HtmlBuilder hb = new HtmlBuilder();

            TagBuilder tag = new TagBuilder("div");
            tag.AddCssClass(Manager.AddOnManager.CheckInvokedCssModule(Forms.CssFormPartial));
            string divId = null;
            if (Manager.IsPostRequest) {
                divId = Manager.UniqueId();
                tag.Attributes.Add("id", divId);
            } else {
                if (UsePartialFormCss && !Manager.IsInPopup && Manager.ActiveDevice != YetaWFManager.DeviceSelected.Mobile &&
                        !string.IsNullOrWhiteSpace(Manager.SkinInfo.PartialFormCss) && module.UsePartialFormCss)
                    tag.AddCssClass(Manager.SkinInfo.PartialFormCss);
            }
            hb.Append(tag.ToString(TagRenderMode.StartTag));

            hb.Append(htmlHelper.AntiForgeryToken());
            hb.Append(htmlHelper.Hidden(Basics.ModuleGuid, module.ModuleGuid));
            hb.Append(htmlHelper.Hidden(Forms.UniqueIdPrefix, Manager.UniqueIdPrefix));

            hb.Append(htmlHelper.ValidationSummary());

            viewHtml = ProcessImages(viewHtml);
            hb.Append(viewHtml);

            hb.Append(tag.ToString(TagRenderMode.EndTag));

            if (divId != null)
                Manager.ScriptManager.AddLast(string.Format("YetaWF_Forms.initPartialForm($('#{0}'));", divId));

            Variables vars = new Variables(Manager) { DoubleEscape = true, CurlyBraces = !Manager.EditMode };
            return vars.ReplaceModuleVariables(module, hb.ToString());
        }

        private static string ProcessImages(string viewHtml) {
            if (!Manager.IsPostRequest) return viewHtml; // we'll handle it in RazorPage::PostProcessHtml
            if (Manager.CurrentSite.CanUseCDN || Manager.CurrentSite.CanUseStaticDomain)
                return ImageSupport.ProcessImagesAsCDN(viewHtml);
            return viewHtml;
        }
    }
}
