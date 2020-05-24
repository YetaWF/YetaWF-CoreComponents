/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace YetaWF.Core.Support {

    /// <summary>
    ///Asp.net core translates fields with whitespace to null fields (WHY!!!!), so undo this dumb behavior we never had before with this custom metadata provider
    /// </summary>
    public class WhitespaceMetadataProvider : IDisplayMetadataProvider {

        public WhitespaceMetadataProvider() {}

        public void CreateDisplayMetadata(DisplayMetadataProviderContext context) {
            if (context.Key.MetadataKind == ModelMetadataKind.Property)
                context.DisplayMetadata.ConvertEmptyStringToNull = false;
        }
    }
}
