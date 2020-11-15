/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using YetaWF.Core.Packages;
using System;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using System.Reflection;

namespace YetaWF.Core.Models.Attributes {

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class AreaConventionAttribute : System.Attribute, IControllerModelConvention {

        public void Apply(ControllerModel ctrlModel) {

            //string debug = ctrlModel.ControllerType.FullName;

            string? area = Lookup(ctrlModel.ControllerType);
            if (area != null && !ctrlModel.RouteValues.ContainsKey("area"))
                ctrlModel.RouteValues.Add("area", area); // add our area name based on the package containing the controller
        }
        private string? Lookup(TypeInfo typeInfo) {
            Package? package = Package.TryGetPackageFromType(typeInfo.AsType());
            return package?.AreaName;
        }
    }
}
