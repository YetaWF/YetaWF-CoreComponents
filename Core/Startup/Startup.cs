/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YetaWF.Core.Addons;
using YetaWF.Core.Log;
using YetaWF.Core.Packages;

namespace YetaWF.Core.Support {

    public interface IInitializeApplicationStartup { // any class defining this interface is called during application startup
        Task InitializeApplicationStartupAsync();
    }

    public static class Startup {

        public static bool Started { get; set; }

        public static async Task CallStartupClassesAsync() {
            Logging.AddLog("Processing IInitializeApplicationStartup");
            // get all types, but put the VersionManagerStartup class first
            List<Type> types = Package.GetClassesInPackages<IInitializeApplicationStartup>(OrderByServiceLevel: true);
            Type versionManager = typeof(VersionManagerStartup);
            types.Remove(versionManager);
            types.Insert(0, versionManager);
            // now start up all classes
            foreach (Type type in types) {
                try {
                    IInitializeApplicationStartup iStart = (IInitializeApplicationStartup)Activator.CreateInstance(type);
                    if (iStart != null) {
                        Logging.AddLog("Calling global startup class \'{0}\'", type.FullName);
                        await iStart.InitializeApplicationStartupAsync();
                    }
                } catch (Exception exc) {
                    Logging.AddErrorLog("Startup class {0} failed.", type.FullName, exc);
                    throw;
                }
            }
            Logging.AddLog("Processing IInitializeApplicationStartup Ended");
        }
    }
}
