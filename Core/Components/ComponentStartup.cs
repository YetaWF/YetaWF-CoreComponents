/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YetaWF.Core.Log;
using YetaWF.Core.Packages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Components {

    /// <summary>
    /// Locates all available components.
    /// </summary>
    /// <remarks>
    /// An instance of this class is instantiated by the framework during application startup and the InitializeApplicationStartupAsync method is called to
    /// locate all available components.
    /// </remarks>
    public class YetaWFComponentBaseStartup : IInitializeApplicationStartup {

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
        /// Called during application startup.
        /// </summary>
        public Task InitializeApplicationStartupAsync() {

            Logging.AddLog("Locating components");

            List<Type> types = Package.GetClassesInPackages<YetaWFComponentBase>();
            foreach (Type tp in types) {

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
    }
}
