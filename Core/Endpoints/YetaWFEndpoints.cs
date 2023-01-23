using System;
using YetaWF.Core.Packages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Endpoints {

    public class YetaWFEndpoints {

        protected static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        /// <summary>
        /// Format a Url for an enpoint, derived from the package, endpoint class and endpoint action.
        /// </summary>
        /// <param name="package">The package.</param>
        /// <param name="type">The type of the class implementing the endpoint. The class name must end in "Endpoint".</param>
        /// <param name="endpoint">The name of the endpoint action.</param>
        /// <returns>A formatted Url to access the endpoint.</returns>
        /// <exception cref="InternalError"></exception>
        protected static string GetEndpoint(Package package, Type type, string endpoint) {
            string className = type.Name;
            if (className.EndsWith("Endpoint"))
                className = className.Substring(0, className.Length - "Endpoint".Length);
            else if (className.EndsWith("Endpoints"))
                className = className.Substring(0, className.Length - "Endpoints".Length);
            else
                throw new InternalError($"Class {className} is not an endpoint");
            return $"{Globals.ApiPrefix}{package.AreaName}/{className}/{endpoint}";
        }
    }
}
