/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YetaWF.Core.Addons;
using YetaWF.Core.DataProvider;
using YetaWF.Core.Localize;
using YetaWF.Core.Models;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Packages;
using YetaWF.Core.Support;
using YetaWF.Core.Support.Repository;
using YetaWF.Core.IO;
#if MVC6
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
#else
#endif

namespace YetaWF.Core.Components {

    /// <summary>
    /// This static class implements services used by the PropertyList component.
    /// </summary>
    public static class PropertyList {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(PropertyList), name, defaultValue, parms); }

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        /// <summary>
        /// Defines the appearance of a property list.
        /// </summary>
        public enum PropertyListStyleEnum {
            /// <summary>
            /// Render a tabbed property list (if there are multiple categories) or a simple list (0 or 1 category).
            /// </summary>
            Tabbed = 0,
            /// <summary>
            /// Render a boxed property list (if there are multiple categories) or a simple list (0 or 1 category), to be styled using CSS.
            /// </summary>
            Boxed = 1,
            /// <summary>
            /// Render a boxed property list with category labels (if there are multiple categories) or a simple list (0 or 1 category), to be styled using CSS.
            /// </summary>
            BoxedWithCategories = 2,
        }

        /// <summary>
        /// An instance of this class defines the property list appearance.
        /// </summary>
        public class PropertyListSetup {
            /// <summary>
            /// The style of the property list.
            /// </summary>
            public PropertyListStyleEnum Style { get; set; }
            /// <summary>
            /// For Boxed and BoxedWithCategories styles, Masonry (https://masonry.desandro.com/) is used to support a packed layout of categories.
            /// </summary>
            /// <remarks>This collection defines the number of columns depending on windows size.
            /// By providing a list of break points, Masonry can be called to recalculate the box layout, when switching between window widths which affects the number of columns.
            ///
            /// The first entry defines the minimum width of the window to use Masonry. Below this size, Masonry is not used.
            /// </remarks>
            public List<PropertyListColumnDef> ColumnStyles { get; set; }
            /// <summary>
            /// Categories (boxes) that are expandable/collapsible. May be null or an empty collection, which means no categories are expandable.
            /// </summary>
            public List<string> ExpandableList { get; set; }
            /// <summary>
            /// Category that is initially expanded. May be null which means no category is initially expanded.
            /// </summary>
            public string InitialExpanded { get; set; }

            /// <summary>
            /// Defines whether the propertylist has a definitions file.
            /// </summary>
            public bool ExplicitDefinitions { get; set; }

            /// <summary>
            /// Constructor.
            /// </summary>
            public PropertyListSetup() {
                Style = PropertyListStyleEnum.Tabbed;
                ColumnStyles = new List<PropertyListColumnDef>();
                ExpandableList = new List<string>();
                InitialExpanded = null;
            }
        }
        /// <summary>
        /// An instance of this class defines the number of columns to display based on the defined minimum window width.
        /// </summary>
        public class PropertyListColumnDef {
            /// <summary>
            /// The minimum window size where the specified number of columns is displayed.
            /// </summary>
            public int MinWindowSize { get; set; }
            /// <summary>
            /// The number of columns to display. Valid values are 1 through 5.
            /// </summary>
            public int Columns { get; set; }
        }

        /// <summary>
        /// Loads the propertylist definitions for a propertylist based on its model type.
        /// </summary>
        /// <param name="model">The model type for which propertylist definitions is to be loaded.</param>
        /// <returns>Returns an object describing the propertylist.</returns>
        /// <remarks>This method is not used by applications. It is reserved for component implementation.</remarks>
        public static async Task<PropertyListSetup> LoadPropertyListDefinitionsAsync(Type model) {
            string className = model.FullName.Split(new char[] { '.' }).Last();
            string[] s = className.Split(new char[] { '+' });
            int len = s.Length;
            if (len != 2) throw new InternalError($"Unexpected class {className} in propertylist model {model.FullName}");
            string controller = s[0];
            string objClass = s[1];
            string file = controller + "." + objClass;

            Package package = Package.GetPackageFromType(model);
            string predefUrl = VersionManager.GetAddOnPackageUrl(package.AreaName) + "PropertyLists/" + file;
            string customUrl = VersionManager.GetCustomUrlFromUrl(predefUrl);
            PropertyListSetup setup = null;
            PropertyListSetup predefSetup = await ReadPropertyListSetupAsync(package, model, YetaWFManager.UrlToPhysical(predefUrl));
            if (predefSetup.ExplicitDefinitions)
                setup = predefSetup;
            PropertyListSetup customInfo = await ReadPropertyListSetupAsync(package, model, YetaWFManager.UrlToPhysical(customUrl));
            if (customInfo.ExplicitDefinitions)
                setup = customInfo;
            if (setup == null)
                setup = new PropertyListSetup();
            return setup;
        }

        //RESEARCH: this could use some caching
        private static async Task<PropertyListSetup> ReadPropertyListSetupAsync(Package package, Type model, string file) {
            if (YetaWFManager.DiagnosticsMode) {
                if (!await FileSystem.FileSystemProvider.FileExistsAsync(file)) {
                    return new PropertyListSetup {
                        ExplicitDefinitions = false,
                    };
                }
            }

            string text;
            try {
                text = await FileSystem.FileSystemProvider.ReadAllTextAsync(file);
            } catch (Exception) {
                return new PropertyListSetup {
                    ExplicitDefinitions = false,
                };
            }

            PropertyListSetup setup = YetaWFManager.JsonDeserialize<PropertyListSetup>(text);
            setup.ExplicitDefinitions = true;
            return setup;
        }
    }
}