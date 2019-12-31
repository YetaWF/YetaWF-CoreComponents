/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

#if MVC6
#else

using Microsoft.Owin;
using Owin;
using System;
using System.Collections.Generic;
using YetaWF.Core.Log;
using YetaWF.Core.Packages;

[assembly: OwinStartup(typeof(YetaWF.Core.Support.OwinStartup))]
namespace YetaWF.Core.Support {

    /// <summary>
    /// Classes that implement this interface are called during application startup.
    /// </summary>
    /// <remarks>This is only used for ASP.NET. ASP.NET Core does not use the IInitializeOwinStartup interface.</remarks>
    public interface IInitializeOwinStartup {
        void InitializeOwinStartup(IAppBuilder app);
    }

    /// <summary>
    /// An instance of this class is instantiated during application startup and the Configuration method is called.
    /// The Configuration method instantiates all classes that implement the IInitializeOwinStartup interface and calls their InitializeOwinStartup method.
    /// </summary>
    public class OwinStartup {

        public void Configuration(IAppBuilder app) {

            Logging.AddLog("Processing OwinStartup");
            List<Type> types = Package.GetClassesInPackages<IInitializeOwinStartup>(OrderByServiceLevel: true);
            // start up all classes
            foreach (Type type in types) {
                try {
                    IInitializeOwinStartup iStart = (IInitializeOwinStartup)Activator.CreateInstance(type);
                    if (iStart != null) {
                        Logging.AddLog("Calling Owin startup class \'{0}\'", type.FullName);
                        iStart.InitializeOwinStartup(app);
                    }
                } catch (Exception exc) {
                    Logging.AddErrorLog("Owin startup class {0} failed.", type.FullName, exc);
                    throw;
                }
            }
            Logging.AddLog("Processing OwinStartup Ended");
        }
    }
}
#endif
