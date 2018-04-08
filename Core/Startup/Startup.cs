﻿/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YetaWF.Core.Addons;
using YetaWF.Core.IO;
using YetaWF.Core.Log;
using YetaWF.Core.Packages;

namespace YetaWF.Core.Support {

    public interface IInitializeApplicationStartup { // any class defining this interface is called during application startup
        Task InitializeApplicationStartupAsync();
    }
    public interface IInitializeApplicationStartupFirstNodeOnly { // any class defining this interface is called during application startup of the FIRST NODE only
        Task InitializeFirstNodeStartupAsync();
    }

    public static class Startup {

        /// <summary>
        /// Defines whether the current instance has fully initialized.
        /// </summary>
        public static bool Started { get; set; }

        private const string MULTIINSTANCESTARTTIMEKEY = "__MultiInstanceStartTime";

        public static DateTime MultiInstanceStartTime { get; private set; }
        public static bool RestartPending { get; set; }

        /// <summary>
        /// Defines whether this is a multi-instance site (i.e., web farm, web garden) using cache sharing/file system sharing.
        /// </summary>
        /// <remarks>Web farm/garden are only possible if shared caching is implemented (MultiInstance is true).
        /// If MultiInstance is true, cached data and the file system is shared between multiple site instances, otherwise only one instance is allowed.</remarks>
        public static bool MultiInstance { get; set; }

        public const string FirstNodeIndicator = "FirstNode.txt";

        public static async Task CallStartupClassesAsync() {

            Logging.AddLog("Processing IInitializeApplicationStartup");

            List<Type> types = Package.GetClassesInPackages<IInitializeApplicationStartup>(OrderByServiceLevel: true);

            List<Type> baseTypes;
            // Start up all classes that are need for basic support. These cannot use "FirstNode" information as we don't yet know whether this is the first node.
            baseTypes = Package.GetClassesInPackages<IInitializeApplicationStartup>(ServiceLevel: ServiceLevelEnum.CachingProvider);
            Logging.AddLog("Processing ServiceLevelEnum.CachingProvider");
            await StartTypesAsync(baseTypes);
            types = types.Except(baseTypes).ToList();

            // make sure the required providers are installed
            if (YetaWF.Core.IO.Caching.GetLocalCacheProvider == null)
                throw new InternalError("There is no local cache provider");
            if (YetaWF.Core.IO.Caching.GetSharedCacheProvider == null)
                throw new InternalError("There is no shared cache provider");
            if (YetaWF.Core.IO.Caching.GetStaticCacheProvider == null)
                throw new InternalError("There is no static cache provider");
            
            // Now we need to determine whether this is the first node
            // We have an indicator file ./Data/FirstNode.txt that signals that the first startup is a "first node".
            // This file must be deployed with the site to force a new start.
            // see if this is the first node

            bool firstNode = false;

            // lock on the first node indicator file until we're completely initialized so no other instance can run (we're updating shared resources)
            string rootFolder;
#if MVC6
            rootFolder = YetaWFManager.RootFolderWebProject;
#else
            rootFolder = YetaWFManager.RootFolder;
#endif
            string file = Path.Combine(rootFolder, Globals.DataFolder, FirstNodeIndicator);
            using (ILockObject lockObject = await YetaWF.Core.IO.FileSystem.FileSystemProvider.LockResourceAsync(file)) {
                if (!YetaWF.Core.Support.Startup.MultiInstance || await YetaWF.Core.IO.FileSystem.FileSystemProvider.FileExistsAsync(file))
                    firstNode = true;

                MultiInstanceStartTime = DateTime.UtcNow;
                if (!firstNode) {
                    // read the startup info object and make sure there was a first node
                    using (ICacheDataProvider staticCacheDP = YetaWF.Core.IO.Caching.GetStaticCacheProvider()) {
                        GetObjectInfo<StartupInfoObject> info = await staticCacheDP.GetAsync<StartupInfoObject>(MULTIINSTANCESTARTTIMEKEY);
                        if (!info.Success)
                            firstNode = true;
                        else
                            MultiInstanceStartTime = info.Data.MultiInstanceStartTime;
                    }
                }

                // run first node specific initialization
                baseTypes = Package.GetClassesInPackages<IInitializeApplicationStartupFirstNodeOnly>(ServiceLevel: ServiceLevelEnum.CachingProvider);
                Logging.AddLog("Processing ServiceLevelEnum.Core");
                await StartTypesAsync(baseTypes, Allow: AllowRun.NodeSpecificOnly, FirstNode: firstNode);
                types = types.Except(baseTypes).ToList();

                baseTypes = Package.GetClassesInPackages<IInitializeApplicationStartup>(ServiceLevel: ServiceLevelEnum.Core);
                baseTypes.AddRange(Package.GetClassesInPackages<IInitializeApplicationStartupFirstNodeOnly>(ServiceLevel: ServiceLevelEnum.Core));
                Type versionManager = typeof(VersionManagerStartup);// put the VersionManagerStartup class first
                baseTypes.Remove(versionManager);
                baseTypes.Insert(0, versionManager);
                Logging.AddLog("Processing ServiceLevelEnum.Core");
                await StartTypesAsync(baseTypes, Allow: AllowRun.Both, FirstNode: firstNode);
                types = types.Except(baseTypes).ToList();

                // Start up everything else
                Logging.AddLog("Processing remaining classes");
                await StartTypesAsync(types, Allow: AllowRun.Both, FirstNode: firstNode);

                if (firstNode) {
                    // set the start time (also used as cache buster)
                    StartupInfoObject startTime = new Support.Startup.StartupInfoObject() {
                        MultiInstanceStartTime = MultiInstanceStartTime,
                    };
                    using (ICacheDataProvider staticCacheDP = YetaWF.Core.IO.Caching.GetStaticCacheProvider()) {
                        await staticCacheDP.AddAsync(MULTIINSTANCESTARTTIMEKEY, startTime);
                    }
                }

                await YetaWF.Core.IO.FileSystem.FileSystemProvider.DeleteFileAsync(file);
                await lockObject.UnlockAsync();

                await YetaWF.Core.Audit.Auditing.AddAuditAsync($"{nameof(Startup)}.{nameof(CallStartupClassesAsync)}",
                    firstNode ? "Site Start First Node" : "Site Start", 
                    Guid.Empty,
                    firstNode ? "Site Started (all instanced)" : "Site Started",
                    ExpensiveMultiInstance: true
                );
            }
            Logging.AddLog("Processing IInitializeApplicationStartup Ended");
        }

        private class StartupInfoObject {
            public DateTime MultiInstanceStartTime { get; set; }
        }

        private enum AllowRun {
            Both = 0,
            NodeSpecificOnly = 1,
            UnknownNodeOnly = 2,
        };

        private static async Task StartTypesAsync(List<Type> types, AllowRun Allow = AllowRun.UnknownNodeOnly, bool FirstNode = false) {
            // now start up all classes
            foreach (Type type in types) {
                try {
                    object obj = Activator.CreateInstance(type);
                    IInitializeApplicationStartupFirstNodeOnly iStartSpecific = obj as IInitializeApplicationStartupFirstNodeOnly;
                    if ((Allow == AllowRun.Both || Allow == AllowRun.NodeSpecificOnly) && FirstNode && iStartSpecific != null) {
                        Logging.AddLog("Calling global startup class \'{0}\' - node specific", type.FullName);
                        await iStartSpecific.InitializeFirstNodeStartupAsync();
                        continue;
                    }
                    IInitializeApplicationStartup iStart = obj as IInitializeApplicationStartup;
                    if ((Allow == AllowRun.Both || Allow == AllowRun.UnknownNodeOnly) && iStart != null) {
                        Logging.AddLog("Calling global startup class \'{0}\'", type.FullName);
                        await iStart.InitializeApplicationStartupAsync();
                        continue;
                    }
                } catch (Exception exc) {
                    Logging.AddErrorLog($"Startup for class {type.FullName} failed", exc);
                    throw;
                }
            }
        }
    }
}
