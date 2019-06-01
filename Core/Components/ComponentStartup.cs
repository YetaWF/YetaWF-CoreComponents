/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using YetaWF.Core.Log;
using YetaWF.Core.Packages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Components {

    /// <summary>
    /// Locates all available components, views and pages.
    /// </summary>
    /// <remarks>
    /// An instance of this class is instantiated by the framework during application startup and the InitializeApplicationStartupAsync method is called to
    /// locate all available components, views and pages.
    /// </remarks>
    public class YetaWFComponentBaseStartup : IInitializeApplicationStartup {

        public const string CONTROLLERPREPROCESSMETHOD = "ControllerPreprocessActionAsync";

        /// <summary>
        /// List of edit components.
        /// </summary>
        private static Dictionary<string, Type> ComponentsEdit = new Dictionary<string, Type>();
        /// <summary>
        /// List of display components.
        /// </summary>
        private static Dictionary<string, Type> ComponentsDisplay = new Dictionary<string, Type>();
        /// <summary>
        /// List of available views.
        /// </summary>
        private static Dictionary<string, Type> Views = new Dictionary<string, Type>();
        /// <summary>
        /// List of available pages.
        /// </summary>
        private static Dictionary<string, Type> Pages = new Dictionary<string, Type>();
        /// <summary>
        /// Dictionary of components that have a controller preprocessor action.
        /// </summary>
        private static Dictionary<string, MethodInfo> ComponentsWithControllerPreprocessAction = new Dictionary<string, MethodInfo>();

        /// <summary>
        /// Called during application startup.
        /// </summary>
        public Task InitializeApplicationStartupAsync() {

            Logging.AddLog("Locating components");

            List<Type> types = Package.GetClassesInPackages<YetaWFComponentBase>();
            foreach (Type tp in types) {

                // Find all components

                YetaWFComponentBase component = (YetaWFComponentBase) Activator.CreateInstance(tp);
                Package compPackage = Package.GetPackageFromType(tp);
                string templateName;
                if (compPackage.IsCorePackage || compPackage.Product.StartsWith("Components"))
                    templateName = component.GetTemplateName();
                else
                    templateName = $"{compPackage.AreaName}_{component.GetTemplateName()}";
                YetaWFComponentBase.ComponentType compType = component.GetComponentType();

                Logging.AddLog($"Found component {templateName} ({compType}) - {tp.FullName}");

                switch (compType) {
                    case YetaWFComponentBase.ComponentType.Display:
                        ComponentsDisplay.Add(templateName, tp);
                        break;
                    case YetaWFComponentBase.ComponentType.Edit:
                        ComponentsEdit.Add(templateName, tp);
                        break;
                }

                // check if the component has a controller preprocessor action
                // Invoke RenderAsync
                MethodInfo meth = component.GetType().GetMethod(CONTROLLERPREPROCESSMETHOD, BindingFlags.Static| BindingFlags.Public);
                if (meth != null) {
                    ComponentsWithControllerPreprocessAction.Add(templateName, meth);
                }
            }

            Logging.AddLog("Locating views");

            types = Package.GetClassesInPackages<YetaWFViewBase>();
            foreach (Type tp in types) {

                YetaWFViewBase view = (YetaWFViewBase)Activator.CreateInstance(tp);
                Package viewPackage = Package.GetPackageFromType(tp);
                string viewName;
                if (viewPackage.IsCorePackage || viewPackage.Product.StartsWith("Components"))
                    viewName = view.GetViewName();
                else
                    viewName = $"{viewPackage.AreaName}_{view.GetViewName()}";

                Logging.AddLog($"Found view {viewName} - {tp.FullName}");
                Views.Add(viewName, tp);
            }

            Logging.AddLog("Locating pages");

            types = Package.GetClassesInPackages<YetaWFPageBase>();
            foreach (Type tp in types) {

                YetaWFPageBase page = (YetaWFPageBase)Activator.CreateInstance(tp);
                Package pagePackage = Package.GetPackageFromType(tp);
                string pageName = $"{pagePackage.AreaName}_{page.GetPageName()}";

                Logging.AddLog($"Found page {pageName} - {tp.FullName}");
                Pages.Add(pageName, tp);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Returns a dictionary of available display components.
        /// </summary>
        /// <returns>Returns a dictionary of available display components.</returns>
        public static Dictionary<string, Type> GetComponentsDisplay() { return ComponentsDisplay; }
        /// <summary>
        /// Returns a dictionary of available edit components.
        /// </summary>
        /// <returns>Returns a dictionary of available edit components.</returns>
        public static Dictionary<string, Type> GetComponentsEdit() { return ComponentsEdit; }
        /// <summary>
        /// Returns a dictionary of available views.
        /// </summary>
        /// <returns>Returns a dictionary of available views.</returns>
        public static Dictionary<string, Type> GetViews() { return Views; }
        /// <summary>
        /// Returns a dictionary of available pages.
        /// </summary>
        /// <returns>Returns a dictionary of available pages.</returns>
        public static Dictionary<string, Type> GetPages() { return Pages; }
        /// <summary>
        /// Returns a dictionary of components that have a controller preprocessor action.
        /// </summary>
        public static Dictionary<string, MethodInfo> GetComponentsWithControllerPreprocessAction() { return ComponentsWithControllerPreprocessAction; }
    }
}
