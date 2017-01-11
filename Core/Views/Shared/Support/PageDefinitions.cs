/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using YetaWF.Core.Models;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Pages;

namespace YetaWF.Core.Views.Shared {

    public class PageDefinitions<TModel> : RazorTemplate<TModel> { }

    public static class PageDefinitionsHelper {

        public class GridModel {
            [UIHint("Grid")]
            public GridDefinition GridDef { get; set; }
        }

        public class GridDisplay {
            public GridDisplay(PageDefinition m) {
                ObjectSupport.CopyData(m, this);
            }

            [Caption("Url"), Description("The Url used to identify this page")]
            [UIHint("Url"), ReadOnly]
            public string Url { get; set; }

            [Caption("Title"), Description("The page title which will appear as title in the browser window")]
            [UIHint("MultiString"), ReadOnly]
            public MultiString Title { get; set; }

            [Caption("Description"), Description("The page description (not usually visible, entered by page designer, used for search keywords)")]
            [UIHint("MultiString"), ReadOnly]
            public MultiString Description { get; set; }
        }

        public static DataSourceResult GetDataSourceResultDisplay(List<PageDefinition> model) {
            DataSourceResult data = new DataSourceResult {
                Data = (from m in model select new GridDisplay(m)).ToList<object>(),
                Total = model.Count,
            };
            return data;
        }

        public static MvcHtmlString RenderPageDefinitionsDisplay<TModel>(this HtmlHelper<TModel> htmlHelper, string name, List<PageDefinition> model) {

            bool header = htmlHelper.GetControlInfo<bool>("", "Header", true);
            GridModel grid = new GridModel() {
                GridDef = new GridDefinition() {
                    RecordType = typeof(GridDisplay),
                    Data = GetDataSourceResultDisplay(model),
                    SupportReload = false,
                    PageSizes = new List<int>(),
                    InitialPageSize = 10,
                    ShowHeader = header,
                    ReadOnly = true,
                }
            };
            return htmlHelper.DisplayFor(m => grid.GridDef);
        }
    }
}