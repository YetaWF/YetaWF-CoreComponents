/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;
using YetaWF.Core.Models;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;
#if MVC6
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
#else
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;
#endif

namespace YetaWF.Core.Views.Shared {

    public class GridDataOneRecord<TModel> : RazorTemplate<TModel> { }

    public static class RenderGridDataOneRecordHelper {

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

#if MVC6
        public static HtmlString RenderGridDataOneRecord<TModel>(this IHtmlHelper<TModel> htmlHelper, object model) {
#else
        public static HtmlString RenderGridDataOneRecord(this HtmlHelper<object> htmlHelper, object model) {
#endif
            HtmlBuilder hb = new HtmlBuilder();

            // check if the grid is readonly or the record supports a "__editable" grid entry property
            bool recordEnabled = true;
            bool readOnly = true;
            int recordCount = 0;
            // We're coming here from grid.cshtml. We need to "fix" our viewdata so we generate correct field names
            string prefix = "";
            GridDefinition gridDef = Manager.TryGetParentModel(Skip: 1) as GridDefinition;
            GridDefinition.GridEntryDefinition gridEntry = Manager.GetParentModel() as GridDefinition.GridEntryDefinition;
            DataSourceResult dataSrc = Manager.GetParentModel() as DataSourceResult;
            List<PropertyListEntry> hiddenProps = null;
            List<PropertyListEntry> props = null;
            if (gridDef != null) {
                readOnly = gridDef.ReadOnly;
                if (!readOnly) {
                    ObjectSupport.TryGetPropertyValue<bool>(model, "__editable", out recordEnabled, true);
                }
                recordCount = gridDef.RecordCount;
                prefix = htmlHelper.GetDataFieldPrefix(gridDef).ToString();
                hiddenProps = GridHelper.GetHiddenGridProperties(model, gridDef);
                props = GridHelper.GetGridProperties(model, gridDef);
            } else if (dataSrc != null) {
                if (string.IsNullOrWhiteSpace(dataSrc.FieldPrefix)) {
                    readOnly = true;
                } else {
                    readOnly = false;
                    prefix = dataSrc.FieldPrefix;
                }
                recordCount = dataSrc.RecordCount;
                hiddenProps = GridHelper.GetHiddenGridProperties(model);//not sure whether this could fail - if so, handle it
                props = GridHelper.GetGridProperties(model);
            } else if (gridEntry != null) {
                readOnly = false;
                prefix = gridEntry.Prefix;
                recordCount = gridEntry.RecNumber;
                hiddenProps = GridHelper.GetHiddenGridProperties(model);//not sure whether this could fail - if so, handle it
                props = GridHelper.GetGridProperties(model);
            }
#if MVC6
            IModelMetadataProvider metadataProvider = (IModelMetadataProvider) YetaWFManager.ServiceProvider.GetService(typeof(IModelMetadataProvider));
#else
#endif
            int propCount = 0;
            Manager.RenderingGridCount = Manager.RenderingGridCount + 1;
            foreach (PropertyListEntry prop in props) {

                // Swap out the ViewData so we get the names/ids that we want for these objects
                ViewDataDictionary oldVdd = htmlHelper.ViewContext.ViewData;
#if MVC6
                ModelMetadata meta = metadataProvider.GetMetadataForProperty(model.GetType(), prop.Name);
#else
                ModelMetadata meta = ModelMetadataProviders.Current.GetMetadataForProperty(() => prop.Value, model.GetType(), prop.Name);
#endif
                using (new HtmlHelperExtender.ControlInfoOverride(meta.AdditionalValues)) {

                    string output = "";
                    if (propCount > 0)
                        hb.Append(",");
                    hb.Append("\"{0}\":", prop.Name);

                    if (prop.Name == "__highlight") {
                        // check whether the record supports a special "__highlight" property
                        hb.Append(prop.Value is bool && (bool)prop.Value == true ? "true" : "false");
                    } else {
#if MVC6
                        string oldPrefix = htmlHelper.ViewContext.ViewData.TemplateInfo.HtmlFieldPrefix;
                        htmlHelper.ViewContext.ViewData.TemplateInfo.HtmlFieldPrefix = prefix;
#else
                        TemplateInfo oldTemplateInfo = htmlHelper.ViewContext.ViewData.TemplateInfo;
                        htmlHelper.ViewContext.ViewData.TemplateInfo = new TemplateInfo() { HtmlFieldPrefix = prefix };
#endif

                        string propName = "[" + recordCount + "]." + prop.Name;
                        if (!readOnly && prop.Editable && recordEnabled) {
                            output = htmlHelper.Editor(prop.Name, prop.UIHint, propName).AsString();
                            output += htmlHelper.ValidationMessage(propName).AsString();
                        } else {
                            output = htmlHelper.DisplayFor(m => prop.Value, prop.UIHint, propName).AsString();
                        }
                        if (string.IsNullOrWhiteSpace(output)) { output = "&nbsp;"; }

                        if (!readOnly && prop.Editable && hiddenProps != null) {
                            // list hidden properties with the first editable field
                            foreach (var h in hiddenProps)
                                output += htmlHelper.DisplayFor(m => h.Value, "Hidden", "[" + recordCount + "]." + h.Name).AsString();
                            hiddenProps = null;
                        }

                        hb.Append(YetaWFManager.Jser.Serialize(output));
#if MVC6
                        htmlHelper.ViewContext.ViewData.TemplateInfo.HtmlFieldPrefix = oldPrefix;
#else
                        htmlHelper.ViewContext.ViewData.TemplateInfo = oldTemplateInfo;
#endif
                    }
                }
                ++propCount;
            }
            Manager.RenderingGridCount = Manager.RenderingGridCount - 1;

            return hb.ToHtmlString();
        }
    }
}