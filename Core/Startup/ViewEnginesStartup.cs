/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

#if MVC6
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;
#else
using System.Web.Mvc;
#endif
using YetaWF.Core.Log;

//$$$remove

namespace YetaWF.Core.Support {

    public static class ViewEnginesStartup {

        /// <summary>
        /// Update view engines, we're not using any
        /// </summary>
#if MVC6
        public static void Start(IServiceCollection services) {

            services.Configure<RazorViewEngineOptions>(options => {
                options.AreaViewLocationFormats.Clear();
            });
        }
#else
#endif
    }
}


