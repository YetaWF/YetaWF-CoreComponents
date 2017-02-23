/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using YetaWF.Core.Addons;
using YetaWF.Core.Localize;
using YetaWF.Core.Modules;
using YetaWF.Core.Support;
#if MVC6
using Microsoft.AspNetCore.Mvc.Rendering;
#else
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Views.Shared {

    public enum ButtonTypeEnum {
        Submit = 0,
        Cancel = 1,
        Button = 2,
        Empty = 3,
        Apply = 4,
        ConditionalSubmit = 5, /* Like Submit but is removed when we don't have a return url (used together with an apply button) */
        ConditionalCancel = 6, /* Like Cancel but doesn't consider whether we have a return url */
    }

    public class FormButton {

        protected YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        public string Text { get; set; }
        public string Title { get; set; }
        public string Name { get; set; }
        public string Id { get; set; }
        public string CssClass { get; set; }
        public bool Hidden { get; set; }
        public ButtonTypeEnum ButtonType { get; set; }
        public ModuleAction Action { get; private set; }
        public ModuleAction.RenderModeEnum RenderAs { get; set; }

        public FormButton() { }
        public FormButton(ModuleAction action, ModuleAction.RenderModeEnum renderAs = ModuleAction.RenderModeEnum.Button) {
            ButtonType = action != null ? ButtonTypeEnum.Button : ButtonTypeEnum.Empty;
            Action = action;
            RenderAs = renderAs;
        }
        public MvcHtmlString Render() {
            if (ButtonType == ButtonTypeEnum.Empty)
                return MvcHtmlString.Empty;
            if (Action != null) {
                if (RenderAs == ModuleAction.RenderModeEnum.IconsOnly)
                    return Action.RenderAsIcon();
                else
                    return Action.RenderAsButton();
            } else {
                TagBuilder tag = new TagBuilder("input");

                string text = Text;
                switch (ButtonType) {
                    case ButtonTypeEnum.Submit:
                    case ButtonTypeEnum.ConditionalSubmit:
                        if (ButtonType == ButtonTypeEnum.ConditionalSubmit && !Manager.IsInPopup && !Manager.HaveReturnToUrl) {
                            // if we don't have anyplace to return to and we're not in a popup we don't need a submit button
                            return MvcHtmlString.Empty;
                        }
                        if (string.IsNullOrWhiteSpace(text)) text = this.__ResStr("btnSave", "Save");
                        tag.Attributes.Add("type", "submit");
                        break;
                    case ButtonTypeEnum.Apply:
                        if (string.IsNullOrWhiteSpace(text)) text = this.__ResStr("btnApply", "Apply");
                        tag.Attributes.Add("type", "button");
                        tag.Attributes.Add(Forms.CssDataApplyButton, "");
                        break;
                    default:
                    case ButtonTypeEnum.Button:
                        tag.Attributes.Add("type", "button");
                        break;
                    case ButtonTypeEnum.Cancel:
                    case ButtonTypeEnum.ConditionalCancel:
                        if (ButtonType == ButtonTypeEnum.ConditionalCancel && !Manager.IsInPopup && !Manager.HaveReturnToUrl) {
                            // if we don't have anyplace to return to and we're not in a popup we don't need a cancel button
                            return MvcHtmlString.Empty;
                        }
                        if (string.IsNullOrWhiteSpace(text)) text = this.__ResStr("btnCancel", "Cancel");
                        tag.Attributes.Add("type", "button");
                        tag.AddCssClass(Manager.AddOnManager.CheckInvokedCssModule(Forms.CssFormCancel));
                        break;
                }
                if (!string.IsNullOrWhiteSpace(Id))
                    tag.Attributes.Add("id", Id);
                if (!string.IsNullOrWhiteSpace(Name))
                    tag.Attributes.Add("name", Name);
                if (Hidden)
                    tag.Attributes.Add("style", "display:none");
                if (!string.IsNullOrWhiteSpace(Title))
                    tag.Attributes.Add("title", Title);
                tag.Attributes.Add("value", text);
                if (!string.IsNullOrWhiteSpace(CssClass))
                    tag.AddCssClass(CssClass);
                return MvcHtmlString.Create(tag.ToString(TagRenderMode.StartTag));
            }
        }
    }
}
