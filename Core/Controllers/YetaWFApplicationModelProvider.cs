/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

#if MVC6

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using System.Reflection;
using YetaWF.Core.Packages;

namespace YetaWF.Core.Controllers {

    public class YetaWFApplicationModelProvider : DefaultApplicationModelProvider {
        public YetaWFApplicationModelProvider(IOptions<MvcOptions> mvcOptionsAccessor, IModelMetadataProvider modelMetadataProvider) : base(mvcOptionsAccessor, modelMetadataProvider) { }

        // This is called at startup for all controllers - so no need to cache the data we collect
        protected override ControllerModel CreateControllerModel(TypeInfo typeInfo) {
            ControllerModel ctrlModel = base.CreateControllerModel(typeInfo);
            string area = Lookup(typeInfo);
            if (area != null)
                ctrlModel.RouteValues.Add("area", area); // add our area name based on the package containing the controller
            return ctrlModel;
        }
#if NOTUSED // provided for future expansion
        protected override ActionModel CreateActionModel(TypeInfo typeInfo, MethodInfo methodInfo) {
            ActionModel actionModel = base.CreateActionModel(typeInfo, methodInfo);
            return actionModel;
        }
#endif
        private string Lookup(TypeInfo typeInfo) {
            Package package = Package.TryGetPackageFromType(typeInfo.AsType());
            return package?.AreaName;
        }
    }
}
#endif
