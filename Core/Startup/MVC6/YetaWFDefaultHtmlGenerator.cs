/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

#if MVC6

using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Text.Encodings.Web;

namespace YetaWF.Core.Views
{
    public class YetaWFDefaultHtmlGenerator : DefaultHtmlGenerator {

        public YetaWFDefaultHtmlGenerator(
                IAntiforgery antiforgery,
                IOptions<MvcViewOptions> optionsAccessor,
                IModelMetadataProvider metadataProvider,
                IUrlHelperFactory urlHelperFactory,
                HtmlEncoder htmlEncoder,
                ValidationHtmlAttributeProvider validationAttributeProvider) :
                    base(antiforgery, optionsAccessor, metadataProvider, urlHelperFactory, htmlEncoder, validationAttributeProvider) { }

        protected override TagBuilder GenerateInput(
                ViewContext viewContext,
                InputType inputType,
                ModelExplorer modelExplorer,
                string expression,
                object value,
                bool useViewData,
                bool isChecked,
                bool setId,
                bool isExplicitValue,
                string format,
                IDictionary<string, object> htmlAttributes) {
            setId = false;
            return base.GenerateInput(viewContext, inputType, modelExplorer, expression, value, useViewData, isChecked, setId, isExplicitValue, format, htmlAttributes);
        }
    }
}
#else
#endif
