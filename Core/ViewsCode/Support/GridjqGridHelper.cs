﻿/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YetaWF.Core.Addons;
using YetaWF.Core.Models;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;
using YetaWF.Core.Localize;
#if MVC6
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
#else
using System.Web;
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Views.Shared {

    public class GridjqGrid<TModel> : RazorTemplate<TModel> { }

    public static class GridjqGridHelper {

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }
#if MVC6
        public static HtmlString RenderColNames(this IHtmlHelper htmlHelper, GridDefinition gridDef) {
#else
        public static HtmlString RenderColNames(this HtmlHelper<object> htmlHelper, GridDefinition gridDef) {
#endif
            ScriptBuilder sb = new ScriptBuilder();

            string sortCol = null;
            GridDefinition.SortBy sortDir = GridDefinition.SortBy.NotSpecified;
            Dictionary<string, GridColumnInfo> dict = GridHelper.LoadGridColumnDefinitions(gridDef, ref sortCol, ref sortDir);

            foreach (var d in dict) {
                string propName = d.Key;
                GridColumnInfo gridCol = d.Value;

                var prop = ObjectSupport.GetPropertyData(gridDef.RecordType, propName);

                string caption = prop.GetCaption(gridDef.ResourceRedirect);
                if (!gridCol.Hidden && gridDef.ResourceRedirect != null && string.IsNullOrWhiteSpace(caption))
                    continue;// we need a caption if we're using resource redirects

                string description = prop.GetDescription(gridDef.ResourceRedirect);
                if (string.IsNullOrWhiteSpace(description))
                    sb.Append("'<span>{0}</span>',", YetaWFManager.HtmlEncode(caption));
                else
                    sb.Append("'<span {0}=\"{1}\">{2}</span>',", Basics.CssTooltip, YetaWFManager.HtmlEncode(description), YetaWFManager.HtmlEncode(caption));
            }
            sb.RemoveLast(); // remove last comma
            return sb.ToHtmlString();
        }
        public class GetColModelInfo {
            public HtmlString Data { get; set; }
            public bool HasFilters { get; set; }
        }
#if MVC6
        public static async Task<GetColModelInfo> GetColModelAsync(this IHtmlHelper htmlHelper, GridHelper.GridSavedSettings gridSavedSettings, GridDefinition gridDef) {
#else
        public static async Task<GetColModelInfo> GetColModelAsync(this HtmlHelper<object> htmlHelper, GridHelper.GridSavedSettings gridSavedSettings, GridDefinition gridDef) {
#endif
            ScriptBuilder sb = new ScriptBuilder();

            string sortCol = null;
            GridDefinition.SortBy sortDir = GridDefinition.SortBy.NotSpecified;
            Dictionary<string, GridColumnInfo> dict = GridHelper.LoadGridColumnDefinitions(gridDef, ref sortCol, ref sortDir);

            bool hasFilters = false;

            foreach (var d in dict) {
                string propName = d.Key;
                GridColumnInfo gridCol = d.Value;

                PropertyData prop = ObjectSupport.GetPropertyData(gridDef.RecordType, propName);

                string caption = prop.GetCaption(gridDef.ResourceRedirect);
                if (!gridCol.Hidden && gridDef.ResourceRedirect != null && string.IsNullOrWhiteSpace(caption))
                    continue;// we need a caption if we're using resource redirects

                sb.Append("{");
                sb.Append("name:{0},index:{0},", YetaWFManager.JsonSerialize(prop.Name));

                int width = 0;
                if (gridCol.Icons != 0) {
                    gridCol.Sortable = false;
                    GridHelper.GridActionsEnum actionStyle = GridHelper.GridActionsEnum.Icons;
                    if (gridCol.Icons > 1)
                        actionStyle = UserSettings.GetProperty<GridHelper.GridActionsEnum>("GridActions");
                    gridCol.ChWidth = gridCol.PixWidth = 0;
                    gridCol.Alignment = GridHAlignmentEnum.Center;
                    if (actionStyle == GridHelper.GridActionsEnum.DropdownMenu) {
                        width = Manager.CharWidthAvg * 12;
                    } else {
                        width = Manager.CharWidthAvg + (Math.Abs(gridCol.Icons) * (16 + Manager.CharWidthAvg / 2 + 2) + Manager.CharWidthAvg);
                    }
                }
                if (gridCol.ChWidth != 0)
                    width = gridCol.ChWidth * Manager.CharWidthAvg + Manager.CharWidthAvg / 2;
                else if (gridCol.PixWidth != 0)
                    width = gridCol.PixWidth;

                if (gridSavedSettings != null && gridSavedSettings.Columns.ContainsKey(prop.Name)) {
                    GridDefinition.ColumnInfo columnInfo = gridSavedSettings.Columns[prop.Name];
                    if (columnInfo.Width >= 0)
                        width = columnInfo.Width; // override calculated width
                }
                sb.Append("has_form_data:{0},", YetaWFManager.JsonSerialize(!prop.ReadOnly));
                if (!prop.ReadOnly)
                    sb.Append("no_sub_if_notchecked:{0},", YetaWFManager.JsonSerialize(gridCol.OnlySubmitWhenChecked));

                sb.Append("width:{0},", width);
                sb.Append("title: false,");

                sb.Append("classes:'t_cell t_{0}',", prop.Name.ToLower());
                switch (gridCol.Alignment) {
                    case GridHAlignmentEnum.Unspecified:
                    case GridHAlignmentEnum.Left:
                        break;
                    case GridHAlignmentEnum.Center:
                        sb.Append("align:'center',");
                        break;
                    case GridHAlignmentEnum.Right:
                        sb.Append("align:'right',");
                        break;
                }
                if (!gridCol.Sortable)
                    sb.Append("sortable:false,");
                if (gridCol.Hidden)
                    sb.Append("hidden:true,");
                if (!gridCol.Locked)
                    sb.Append("resizable:true,");
                if (gridCol.FilterOptions.Count > 0 && gridDef.ShowFilter != false) {
                    hasFilters = true;
                    sb.Append("search:true,");
                    if (prop.PropInfo.PropertyType == typeof(Boolean) || prop.PropInfo.PropertyType == typeof(Boolean?)) {
                        sb.Append("stype:'select',searchoptions:{value:'");
                        sb.Append(":All;True:Yes;False:No");
                        sb.Append("'},");
                    } else if (prop.PropInfo.PropertyType == typeof(int) || prop.PropInfo.PropertyType == typeof(int?) || prop.PropInfo.PropertyType == typeof(long) || prop.PropInfo.PropertyType == typeof(long?)) {
                        sb.Append("stype:'text',searchoptions:{sopt:['ge','gt','le','lt','eq','ne']},searchrules:{integer:true},");
                    } else if (prop.PropInfo.PropertyType == typeof(decimal) || prop.PropInfo.PropertyType == typeof(decimal?)) {
                        sb.Append("stype:'text',searchoptions:{sopt:['ge','gt','le','lt','eq','ne']},searchrules:{integer:true},");
                    } else if (prop.PropInfo.PropertyType == typeof(DateTime) || prop.PropInfo.PropertyType == typeof(DateTime?)) {
                        sb.Append("searchoptions:{sopt:['ge','le'],dataInit: function(elem) {");
                        if (prop.UIHint == "DateTime") {
                            sb.Append(DateTimeHelper.RenderDateTimeJavascript(gridDef.Id, "elem"));
                        } else if (prop.UIHint == "Date") {
                            sb.Append(DateHelper.RenderDateJavascript(gridDef.Id, "elem"));
                        } else {
                            throw new InternalError("Need DateTime or Date UIHint for DateTime data");
                        }
                        sb.Append(" },},");
                    } else if (prop.PropInfo.PropertyType == typeof(Guid) || prop.PropInfo.PropertyType == typeof(Guid?)) {
                        sb.Append("stype:'text',searchoptions:{sopt:['cn','bw','ew']},");
                    } else if (prop.PropInfo.PropertyType.IsEnum) {
                        sb.Append("stype:'select',searchoptions:{sopt:['eq','ne'],value:':(no selection)");
                        EnumData enumData = ObjectSupport.GetEnumData(prop.PropInfo.PropertyType);
                        foreach (EnumDataEntry entry in enumData.Entries) {
                            string capt = YetaWFManager.JsonSerialize(entry.Caption);
                            capt = capt.Substring(1, capt.Length - 2);
                            sb.Append(";{0}:{1}", (int)entry.Value, capt);
                        }
                        sb.Append("'},");
                    } else {
                        sb.Append("stype:'text',searchoptions:{");
                        AddFilterOptions(sb, gridCol);
                        sb.Append("},");
                    }
                } else {
                    sb.Append("search:false,");
                }
                sb.Append("},");

                // get the uihint to add the template
                if (prop.UIHint != null)
                    await Manager.AddOnManager.AddTemplateFromUIHintAsync(prop.UIHint);
            }
            sb.RemoveLast(); // remove last comma
            return new GetColModelInfo {
                HasFilters = hasFilters,
                Data = sb.ToHtmlString()
            };
        }

        private static void AddFilterOptions(ScriptBuilder sb, GridColumnInfo gridCol) {
            sb.Append("sopt:[");
            foreach (GridColumnInfo.FilterOptionEnum f in gridCol.FilterOptions) {
                sb.Append("'");
                switch (f) {
                    case GridColumnInfo.FilterOptionEnum.Equal: sb.Append("eq"); break;
                    case GridColumnInfo.FilterOptionEnum.GreaterEqual: sb.Append("ge"); break;
                    case GridColumnInfo.FilterOptionEnum.GreaterThan: sb.Append("gt"); break;
                    case GridColumnInfo.FilterOptionEnum.LessEqual: sb.Append("le"); break;
                    case GridColumnInfo.FilterOptionEnum.LessThan: sb.Append("lt"); break;
                    case GridColumnInfo.FilterOptionEnum.NotEqual: sb.Append("ne"); break;
                    case GridColumnInfo.FilterOptionEnum.StartsWith: sb.Append("bw"); break;
                    case GridColumnInfo.FilterOptionEnum.NotStartsWith: sb.Append("bn"); break;
                    case GridColumnInfo.FilterOptionEnum.Contains: sb.Append("cn"); break;
                    case GridColumnInfo.FilterOptionEnum.NotContains: sb.Append("nc"); break;
                    case GridColumnInfo.FilterOptionEnum.Endswith: sb.Append("ew"); break;
                    case GridColumnInfo.FilterOptionEnum.NotEndswith: sb.Append("en"); break;
                    default:
                        throw new InternalError("Unexpected filter option {0}", f);
                }
                sb.Append("',");
            }
            sb.Append("],");
        }

        /// <summary>
        /// Renders the grid sort order
        /// </summary>
#if MVC6
        public static HtmlString RenderGridSortOrder(this IHtmlHelper htmlHelper, GridHelper.GridSavedSettings gridSavedSettings, GridDefinition gridDef) {
#else
        public static HtmlString RenderGridSortOrder(this HtmlHelper<object> htmlHelper, GridHelper.GridSavedSettings gridSavedSettings, GridDefinition gridDef) {
#endif
            GridDefinition.ColumnDictionary columns = null;

            if (gridSavedSettings != null && gridSavedSettings.Columns.Count > 0) // use the saved sort order
                columns = gridSavedSettings.Columns;
            else {
                string sortCol = null;
                GridDefinition.SortBy sortDir = GridDefinition.SortBy.NotSpecified;
                Dictionary<string, GridColumnInfo> dict = GridHelper.LoadGridColumnDefinitions(gridDef, ref sortCol, ref sortDir);
                if (!string.IsNullOrWhiteSpace(sortCol)) { // use the default sort order
                    columns = new GridDefinition.ColumnDictionary();
                    columns.Add(sortCol, new GridDefinition.ColumnInfo() { Sort = sortDir });
                }
            }
            if (columns == null)
                return HtmlStringExtender.Empty;

            ScriptBuilder sb = new ScriptBuilder();
            foreach (var col in columns) {
                bool found = false;
                switch (col.Value.Sort) {
                    default:
                    case GridDefinition.SortBy.NotSpecified:
                        break;
                    case GridDefinition.SortBy.Ascending:
                        sb.Append("sortname:'{0}',sortorder:'{1}',", col.Key, "asc");
                        found = true;
                        break;
                    case GridDefinition.SortBy.Descending:
                        sb.Append("sortname:'{0}',sortorder:'{1}',", col.Key, "desc");
                        found = true;
                        break;
                }
                if (found) break;// only one column supported in jqgrid
            }

            return sb.ToHtmlString();
        }
    }
}