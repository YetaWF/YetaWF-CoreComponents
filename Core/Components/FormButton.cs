/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

#nullable enable

using System.Threading.Tasks;
using YetaWF.Core.Modules;

namespace YetaWF.Core.Components {

    /// <summary>
    /// The form button type.
    /// </summary>
    public enum ButtonTypeEnum {
        /// <summary>
        /// A button that submits the form.
        /// </summary>
        Submit = 0,
        /// <summary>
        /// A button that cancels the form.
        /// </summary>
        Cancel = 1,
        /// <summary>
        /// A button that has no action on the form.
        /// This is typically used by client-side code.
        /// </summary>
        Button = 2,
        /// <summary>
        /// A button that has no action on the form.
        /// It is not rendered.
        /// </summary>
        Empty = 3,
        /// <summary>
        /// A button that submits the form.
        /// </summary>
        Apply = 4,
        /// <summary>
        /// A button that submits the form.
        /// This button type is only shown if the page has a valid URL to return to.
        /// </summary>
        ConditionalSubmit = 5, /* Like Submit but is removed when we don't have a return url (must be used together with an apply button) */
    }

    /// <summary>
    /// An instance of this class represents a button as used within a &lt;form&gt; tag.
    /// </summary>
    public class FormButton {

        /// <summary>
        /// The button text.
        /// </summary>
        public string? Text { get; set; }
        /// <summary>
        /// The button's tooltip.
        /// </summary>
        public string? Title { get; set; }
        /// <summary>
        /// The HTML name attribute of the button.
        /// </summary>
        public string? Name { get; set; }
        /// <summary>
        /// The HTML id attribute of the button.
        /// </summary>
        public string? Id { get; set; }
        /// <summary>
        /// The HTML CSS classes added to the button.
        /// </summary>
        public string? CssClass { get; set; }
        /// <summary>
        /// Defines whether this is a hidden button (&lt;input type='hidden'&gt;).
        /// </summary>
        public bool Hidden { get; set; }
        /// <summary>
        ///  The type of the button.
        /// </summary>
        public ButtonTypeEnum ButtonType { get; set; }
        /// <summary>
        /// The module action invoked by the button. May be null.
        /// </summary>
        public ModuleAction? Action { get; private set; }
        /// <summary>
        /// Defines the appearance of the button.
        /// </summary>
        public ModuleAction.RenderModeEnum RenderAs { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public FormButton() { }
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="action">The module action invoked by the button. May be null.</param>
        /// <param name="renderAs">Defines the appearance of the button.</param>
        public FormButton(ModuleAction? action, ModuleAction.RenderModeEnum renderAs = ModuleAction.RenderModeEnum.Button) {
            ButtonType = action != null ? ButtonTypeEnum.Button : ButtonTypeEnum.Empty;
            Action = action;
            RenderAs = renderAs;
        }
        /// <summary>
        /// Renders the button HTML.
        /// </summary>
        /// <returns>Returns the button as HTML.</returns>
        public async Task<string> RenderAsync() {
            if (ButtonType == ButtonTypeEnum.Empty)
                return string.Empty;
            if (Action != null) {
                switch (RenderAs) {
                    default:
                    case ModuleAction.RenderModeEnum.NormalMenu:
                    case ModuleAction.RenderModeEnum.Button:
                        return await Action.RenderAsButtonAsync();
                    case ModuleAction.RenderModeEnum.ButtonIcon:
                        return await Action.RenderAsButtonIconAsync();
                    case ModuleAction.RenderModeEnum.ButtonOnly:
                        return await Action.RenderAsButtonOnlyAsync();
                    case ModuleAction.RenderModeEnum.IconsOnly:
                        return await Action.RenderAsIconAsync();
                    case ModuleAction.RenderModeEnum.LinksOnly:
                        return await Action.RenderAsLinkAsync();
                    case ModuleAction.RenderModeEnum.NormalLinks:
                        return await Action.RenderAsNormalLinkAsync();
                }
            } else {
                return await YetaWFCoreRendering.Render.RenderFormButtonAsync(this);
            }
        }
    }
}
