/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

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

    public interface IInitializeOwinStartup { // any class defining this interface is called during application startup
        void InitializeOwinStartup(IAppBuilder app);
    }

    public class OwinStartup {

        public void Configuration(IAppBuilder app) {

            Logging.AddLog("Processing OwinStartup");
            // get all types, but put the VersionManagerStartup class first
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
