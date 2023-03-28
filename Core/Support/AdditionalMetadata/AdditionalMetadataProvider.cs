/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using System.Collections.Generic;

namespace YetaWF.Core.Support;

public interface IAdditionalAttribute {
    void OnAddAdditionalValues(IDictionary<object, object> additionalValues);
}

public class AdditionalMetadataProvider : IDisplayMetadataProvider {

    public AdditionalMetadataProvider() {}

    public void CreateDisplayMetadata(DisplayMetadataProviderContext context) {
        // Extract all AdditionalMetadataAttribute values and add to AdditionalValues
        // This also supports an IMetadataAware replacement named IAdditionalAttribute
        if (context.PropertyAttributes != null) {
            foreach (object propAttr in context.PropertyAttributes) {
                AdditionalMetadataAttribute? addMetaAttr = propAttr as AdditionalMetadataAttribute;
                if (addMetaAttr != null && addMetaAttr.Value != null && !context.DisplayMetadata.AdditionalValues.ContainsKey(addMetaAttr.Name)) {
                    context.DisplayMetadata.AdditionalValues.Add(addMetaAttr.Name, addMetaAttr.Value);
                }
                IAdditionalAttribute? iAddtl = propAttr as IAdditionalAttribute;
                if (iAddtl != null) {
                    iAddtl.OnAddAdditionalValues(context.DisplayMetadata.AdditionalValues);
                }
            }
        }
    }
}
