﻿/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Threading.Tasks;
using YetaWF.Core.Modules;
using YetaWF.Core.Packages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Components {

    /// <summary>
    /// This interface is implemented by packages that render components and views.
    /// </summary>
    /// <remarks>
    /// Components and views are always rendered as HTML with JavaScript and CSS. The actual implementation of the components and views
    /// is determined by the package implementing the component.
    /// The default package YetaWF.ComponentsHTML implements all default components and views.
    ///
    /// Other application packages can also implement their own components and views in their own package folder .\Components and .\Views respectively.
    /// Any components and views an application implements (referencing the YetaWF.ComponentsHTML package) must be located in the
    /// package's .\Components\HTML and .\Views\HTML folders.
    /// </remarks>
    public interface IYetaWFCoreRendering {
        /// <summary>
        /// Returns the package that implements this interface.
        /// </summary>
        /// <returns>Returns the package that implements this interface.</returns>
        Package GetImplementingPackage();
        /// <summary>
        /// Adds any addons that are required by the package rendering components and views.
        /// </summary>
        Task AddStandardAddOnsAsync();
        /// <summary>
        /// Adds any skin-specific addons for the current page that are required by the package rendering components and views.
        /// </summary>
        /// <remarks>This is called before the page is rendered.</remarks>
        Task AddSkinAddOnsAsync();
        /// <summary>
        /// Adds any form-specific addons for the current page that are required by the package rendering components and views.
        /// </summary>
        /// <remarks>This is only called if a page contains a form.</remarks>
        Task AddFormsAddOnsAsync();
        /// <summary>
        /// Adds any popup-specific addons for the current page that are required by the package rendering components and views.
        /// </summary>
        /// <remarks>This is only called if a page can contain a popup.</remarks>
        Task AddPopupsAddOnsAsync();
        /// <summary>
        /// Renders a view.
        /// </summary>
        /// <param name="htmlHelper">The HtmlHelper instance.</param>
        /// <param name="module">The module being rendered in the view.</param>
        /// <param name="viewHtml">The current view contents to be wrapped in the view.</param>
        /// <param name="UsePartialFormCss">Defines whether the partial form CSS should be used.</param>
        /// <returns>Returns the complete view as HTML.</returns>
        Task<string> RenderViewAsync(YHtmlHelper htmlHelper, ModuleDefinition module, string viewHtml, bool UsePartialFormCss);

        /// <summary>
        /// Renders module links.
        /// </summary>
        /// <param name="mod">The module for which the module links are rendered.</param>
        /// <param name="renderMode">The module links' rendering mode.</param>
        /// <param name="cssClass">The optional CSS classes to use for the module links.</param>
        /// <returns>Returns the module links as HTML.</returns>
        Task<string> RenderModuleLinksAsync(ModuleDefinition mod, ModuleAction.RenderModeEnum renderMode, string cssClass);

        /// <summary>
        /// Renders a complete module menu.
        /// </summary>
        /// <param name="mod">The module for which the module menu is rendered.</param>
        /// <returns>Returns the complete module menu as HTML.</returns>
        Task<string> RenderModuleMenuAsync(ModuleDefinition mod);

        /// <summary>
        /// Renders a module action.
        /// </summary>
        /// <param name="action">The module action to render.</param>
        /// <param name="mode">The module action's rendering mode.</param>
        /// <param name="id">The ID to generate.</param>
        /// <returns>Returns the module action as HTML.</returns>
        Task<string> RenderModuleActionAsync(ModuleAction action, ModuleAction.RenderModeEnum mode, string? id, string? cssClass);

        /// <summary>
        /// Renders a form button.
        /// </summary>
        /// <param name="formButton">The form button to render.</param>
        /// <returns>Returns the rendered form button as HTML.</returns>
        Task<string> RenderFormButtonAsync(FormButton formButton);
    }

    /// <summary>
    /// This static class provides the application interface for all rendering, implemented by the package installed to render components and views.
    /// </summary>
    public static class YetaWFCoreRendering {

        /// <summary>
        /// Returns the IYetaWFCoreRendering interface implemented by the package installed to render components and views.
        /// </summary>
        /// <remarks>The package implementing rendering components and views sets this accessor during application startup.
        /// All application rendering is performed using this interface.</remarks>
        public static IYetaWFCoreRendering Render {
            get {
                if (_render == null) throw new InternalError($"No {nameof(IYetaWFCoreRendering)} handler installed");
                return _render;
            }
            set {
                if (_render != null) throw new InternalError($"{nameof(IYetaWFCoreRendering)} handler already installed");
                _render = value;
            }
        }
        private static IYetaWFCoreRendering? _render;
    }
}
