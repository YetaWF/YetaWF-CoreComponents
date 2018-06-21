/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YetaWF.Core.Log;
using YetaWF.Core.Packages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Components {

    public class YetaWFComponentBaseStartup : IInitializeApplicationStartup {

        /// <summary>
        /// List of available templates with the associated component class type.
        /// </summary>
        private static Dictionary<string, Type> ComponentsEdit = new Dictionary<string, Type>();
        private static Dictionary<string, Type> ComponentsDisplay = new Dictionary<string, Type>();
        /// <summary>
        /// List of available views.
        /// </summary>
        private static Dictionary<string, Type> Views = new Dictionary<string, Type>();

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
                    templateName = $"{compPackage.Domain}_{compPackage.Product}_{component.GetTemplateName()}";
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
                    viewName = $"{viewPackage.Domain}_{viewPackage.Product}_{view.GetViewName()}";

                Logging.AddLog($"Found view {viewName} - {tp.FullName}");
                Views.Add(viewName, tp);
            }

            return Task.CompletedTask;
        }

        public static Dictionary<string, Type> GetComponentsDisplay() { return ComponentsDisplay; }
        public static Dictionary<string, Type> GetComponentsEdit() { return ComponentsEdit; }
        public static Dictionary<string, Type> GetViews() { return Views; }
    }
}
