/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Threading.Tasks;
using YetaWF.Core.Modules;
using YetaWF.Core.Support;

namespace YetaWF.Core.Components {

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
        public async Task<YHtmlString> RenderAsync() {
            if (ButtonType == ButtonTypeEnum.Empty)
                return new YHtmlString("");
            if (Action != null) {
                if (RenderAs == ModuleAction.RenderModeEnum.IconsOnly)
                    return await Action.RenderAsIconAsync();
                else
                    return await Action.RenderAsButtonAsync();
            } else {
                return await YetaWFCoreRendering.Render.RenderFormButtonAsync(this);
            }
        }
    }
}
