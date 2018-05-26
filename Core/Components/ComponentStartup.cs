using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YetaWF.Core.Packages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Components {

    public class YetaWFComponentBaseStartup : IInitializeApplicationStartup {

        /// <summary>
        /// List of available templates with the associated component class type.
        /// </summary>
        private static Dictionary<string, Type> ComponentsEdit = new Dictionary<string, Type>();
        private static Dictionary<string, Type> ComponentsDisplay = new Dictionary<string, Type>();

        public Task InitializeApplicationStartupAsync() {
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
                switch (compType) {
                    case YetaWFComponentBase.ComponentType.Display:
                        ComponentsDisplay.Add(templateName, tp);
                        break;
                    case YetaWFComponentBase.ComponentType.Edit:
                        ComponentsEdit.Add(templateName, tp);
                        break;
                }
            }
            return Task.CompletedTask;
        }

        public static Dictionary<string, Type> GetComponentsDisplay() { return ComponentsDisplay; }
        public static Dictionary<string, Type> GetComponentsEdit() { return ComponentsEdit; }
    }
}
