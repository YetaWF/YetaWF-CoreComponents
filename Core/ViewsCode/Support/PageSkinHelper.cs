/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;
using System.Linq;
using System.Text;
using YetaWF.Core.Localize;
using YetaWF.Core.Pages;
using YetaWF.Core.Serializers;
using YetaWF.Core.Skins;
using YetaWF.Core.Support;
#if MVC6
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
#else
using System.Web;
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Views.Shared {

    public class PageSkin<TModel> : RazorTemplate<TModel> { }

    public static class PageSkinHelper {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(PageSkinHelper), name, defaultValue, parms); }
#if MVC6
        public static HtmlString RenderPopupSkinDefinitionDisplay(this IHtmlHelper htmlHelper, string name, SkinDefinition model, object HtmlAttributes = null) {
#else
        public static HtmlString RenderPopupSkinDefinitionDisplay(this HtmlHelper htmlHelper, string name, SkinDefinition model, object HtmlAttributes = null) {
#endif
            TagBuilder tag = new TagBuilder("div");
            htmlHelper.FieldSetup(tag, name, HtmlAttributes: HtmlAttributes, Validation: false, Anonymous: true);

            if (string.IsNullOrWhiteSpace(model.Collection) && model.FileName == SkinAccess.FallbackPopupFileName)
                model.FileName = null;

            SkinAccess skinAccess = new SkinAccess();

            if (string.IsNullOrWhiteSpace(model.Collection) && string.IsNullOrWhiteSpace(model.FileName)) {
                tag.SetInnerHtml("&nbsp;");
            } else {
                StringBuilder sb = new StringBuilder();
                if (string.IsNullOrWhiteSpace(model.Collection))
                    sb.Append(__ResStr("default", "(Default)"));
                else {
                    string collName = (from s in skinAccess.GetAllSkinCollections() where s.CollectionName == model.Collection select s.CollectionDescription).FirstOrDefault();
                    if (string.IsNullOrWhiteSpace(collName))
                        sb.Append(__ResStr("noColl", "(unknown {0})", model.Collection));
                    else
                        sb.Append(collName);
                }
                sb.Append(__ResStr("sep", ", "));
                if (string.IsNullOrWhiteSpace(model.FileName))
                    sb.Append(__ResStr("default", "(Default)"));
                else {
                    string skinName = (from s in skinAccess.GetAllPopupSkins(model.Collection) where s.FileName == model.FileName select s.Name).FirstOrDefault();
                    if (string.IsNullOrWhiteSpace(skinName))
                        sb.Append(__ResStr("noFile", "(unknown {0})", model.FileName));
                    else
                        sb.Append(skinName);
                }
                tag.SetInnerText(sb.ToString());
            }
            return tag.ToHtmlString(TagRenderMode.Normal);
        }
#if MVC6
        public static HtmlString RenderPageSkinDefinitionDisplay(this IHtmlHelper htmlHelper, string name, SkinDefinition model, object HtmlAttributes = null) {
#else
        public static HtmlString RenderPageSkinDefinitionDisplay(this HtmlHelper htmlHelper, string name, SkinDefinition model, object HtmlAttributes = null) {
#endif
            TagBuilder tag = new TagBuilder("div");
            htmlHelper.FieldSetup(tag, name, HtmlAttributes: HtmlAttributes, Validation: false, Anonymous: true);

            if (string.IsNullOrWhiteSpace(model.Collection) && model.FileName == SkinAccess.FallbackPageFileName)
                model.FileName = null;

            SkinAccess skinAccess = new SkinAccess();

            if (string.IsNullOrWhiteSpace(model.Collection) && string.IsNullOrWhiteSpace(model.FileName)) {
                tag.SetInnerHtml("&nbsp;");
            } else {
                StringBuilder sb = new StringBuilder();
                if (string.IsNullOrWhiteSpace(model.Collection))
                    sb.Append(__ResStr("default", "(Default)"));
                else {
                    string collName = (from s in skinAccess.GetAllSkinCollections() where s.CollectionName == model.Collection select s.CollectionDescription).FirstOrDefault();
                    if (string.IsNullOrWhiteSpace(collName))
                        sb.Append(__ResStr("noColl", "(unknown {0})", model.Collection));
                    else
                        sb.Append(collName);
                }
                sb.Append(__ResStr("sep", ", "));
                if (string.IsNullOrWhiteSpace(model.FileName))
                    sb.Append(__ResStr("default", "(Default)"));
                else {
                    string skinName = (from s in skinAccess.GetAllPageSkins(model.Collection) where s.FileName == model.FileName select s.Name).FirstOrDefault();
                    if (string.IsNullOrWhiteSpace(skinName))
                        sb.Append(__ResStr("noFile", "(unknown {0})", model.FileName));
                    else
                        sb.Append(skinName);
                }
                tag.SetInnerText(sb.ToString());
            }
            return tag.ToHtmlString(TagRenderMode.Normal);
        }
#if MVC6
        public static HtmlString RenderSkinCollection(this IHtmlHelper htmlHelper, string name, string selection, object HtmlAttributes = null) {
#else
        public static HtmlString RenderSkinCollection(this HtmlHelper htmlHelper, string name, string selection, object HtmlAttributes = null) {
#endif
            // get all available skins
            SkinAccess skinAccess = new SkinAccess();
            List<SelectionItem<string>> list = (from skinColl in skinAccess.GetAllSkinCollections() orderby skinColl.CollectionDescription select new SelectionItem<string>() {
                Text = skinColl.CollectionDescription,
                Value = skinColl.CollectionName,
            }).ToList();
            bool useDefault = !htmlHelper.GetControlInfo<bool>("", "NoDefault");
            if (useDefault)
                list.Insert(0, new SelectionItem<string> {
                    Text = __ResStr("siteDef", "(Site Default)"),
                    Tooltip = __ResStr("siteDefTT", "Use the site defined default skin"),
                    Value = "",
                });
            // display the skins in a drop down
            return htmlHelper.RenderDropDownSelectionList(name, selection, list, HtmlAttributes: HtmlAttributes);
        }
#if MVC6
        public static HtmlString RenderPageSkinsForCollection(this IHtmlHelper htmlHelper, string name, string selection, string collection, object HtmlAttributes = null) {
#else
        public static HtmlString RenderPageSkinsForCollection(this HtmlHelper htmlHelper, string name, string selection, string collection, object HtmlAttributes = null) {
#endif
        // get all available page skins for this collection
        SkinAccess skinAccess = new SkinAccess();
            PageSkinList skinList = skinAccess.GetAllPageSkins(collection);
            return RenderSkinsForCollection(htmlHelper, name, selection, skinList, HtmlAttributes);
        }
#if MVC6
        public static HtmlString RenderPopupSkinsForCollection(this IHtmlHelper htmlHelper, string name, string selection, string collection, object HtmlAttributes = null) {
#else
        public static HtmlString RenderPopupSkinsForCollection(this HtmlHelper htmlHelper, string name, string selection, string collection, object HtmlAttributes = null) {
#endif
            // get all available popup skins for this collection
            SkinAccess skinAccess = new SkinAccess();
            PageSkinList skinList = skinAccess.GetAllPopupSkins(collection);
            return RenderSkinsForCollection(htmlHelper, name, selection, skinList, HtmlAttributes);
        }
#if MVC6
        private static HtmlString RenderSkinsForCollection(IHtmlHelper htmlHelper, string name, string selection, PageSkinList skinList, object HtmlAttributes)
#else
        private static HtmlString RenderSkinsForCollection(HtmlHelper htmlHelper, string name, string selection, PageSkinList skinList, object HtmlAttributes)
#endif
        {
            List<SelectionItem<string>> list = (from skin in skinList orderby skin.Description select new SelectionItem<string>() {
                Text = skin.Name,
                Tooltip = skin.Description,
                Value = skin.FileName,
            }).ToList();
            // display the skins in a drop down
            return htmlHelper.RenderDropDownSelectionList(name, selection, list, HtmlAttributes: HtmlAttributes);
        }
        public static HtmlString RenderReplacementSkinsForCollection(PageSkinList skinList) {
            List<SelectionItem<string>> list = (from skin in skinList orderby skin.Description select new SelectionItem<string>() {
                    Text = skin.Name,
                    Tooltip = skin.Description,
                    Value = skin.FileName,
                }).ToList();
            // render a new dropdown list
            return DropDownHelper.RenderDataSource(null, list);
        }
#if MVC6
        public static HtmlString RenderModuleSkinsForCollection(this IHtmlHelper htmlHelper, string name, SerializableList<SkinDefinition> model, string collection, object HtmlAttributes = null) {
#else
        public static HtmlString RenderModuleSkinsForCollection(this HtmlHelper htmlHelper, string name, SerializableList<SkinDefinition> model, string collection, object HtmlAttributes = null) {
#endif
            // get all available module skins for this collection
            SkinAccess skinAccess = new SkinAccess();
            ModuleSkinList skinList = skinAccess.GetAllModuleSkins(collection);
            SkinDefinition skinDef = (from s in model where s.Collection == collection select s).FirstOrDefault();
            string selection = (skinDef != null) ? skinDef.FileName : null;
            List<SelectionItem<string>> list = (from skin in skinList select new SelectionItem<string>() {
                Text = skin.Name,
                Tooltip = skin.Description,
                Value = skin.CssClass,
            }).ToList();
            // display the skins in a drop down
            return htmlHelper.RenderDropDownSelectionList(name, selection, list, HtmlAttributes: HtmlAttributes);
        }
    }
}
