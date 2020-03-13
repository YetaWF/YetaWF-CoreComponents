/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Linq;
using YetaWF.Core.Log;
using YetaWF.Core.Packages;
using YetaWF.Core.Support;

namespace YetaWF.Core.DataProvider {

    /// <summary>
    /// This interface is implemented by data providers that need to be registered during application startup.
    /// </summary>
    /// <remarks>This interface is used by the framework to find all data providers during startup. Any data provider that
    /// implements this interface is registered using the Register method.
    ///
    /// Registered data providers can be reviewed using Admin > Dashboard > Data Providers (standard YetaWF site).
    /// </remarks>
    public interface IExternalDataProvider {
        /// <summary>
        /// Called by the framework to register external data providers that expose the YetaWF.Core.DataProvider.IExternalDataProvider interface.
        /// </summary>
        void Register();
    }

    /// <summary>
    /// This static class implements registering all data providers during application startup.
    /// </summary>
    public static class ExternalDataProviders {

        /// <summary>
        /// Called by the framework during application startup to discover and register all data providers, implementing the IExternalDataProvider interface.
        /// </summary>
        public static void RegisterExternalDataProviders() {

            Logging.AddLog($"Processing {nameof(RegisterExternalDataProviders)} - {nameof(IExternalDataProvider)}");

            List<Type> types = Package.GetClassesInPackages<IExternalDataProvider>(OrderByServiceLevel: true);
            foreach (Type type in types) {
                try {
                    IExternalDataProvider iStart = (IExternalDataProvider)Activator.CreateInstance(type);
                    if (iStart != null) {
                        Logging.AddLog($"Calling external data provider startup class \'{type.FullName}\'");
                        iStart.Register();
                    }
                } catch (Exception exc) {
                    Logging.AddErrorLog($"External data provider startup class {type.FullName} failed.", exc);
                    throw;
                }
            }

            Logging.AddLog($"Processing {nameof(RegisterExternalDataProviders)} Ended");
        }
    }

    public abstract partial class DataProviderImpl {

        private string ExternalIOMode { get; set; }

        public class ExternalDataProviderInfo {
            /// <summary>
            /// The type that implements the application data provider.
            /// </summary>
            public Type Type { get; set; }
            /// <summary>
            /// The I/O mode registered by the data provider (e.g, SQL, File).
            /// </summary>
            public string IOModeName { get; set; }
            /// <summary>
            /// The specific object type that is implemented by the low-level data provider.
            /// </summary>
            public Type TypeImpl { get; set; }
        }

        /// <summary>
        /// Creates an external assembly-based data provider.
        /// </summary>
        /// <returns>A data provider object of a type suitable for the data provider.</returns>
        protected dynamic MakeExternalDataProvider(Dictionary<string, object> options, string LimitIOMode = null) {
            if (ExternalIOMode == NoIOMode)
                return null;
            if (LimitIOMode != null && ExternalIOMode != LimitIOMode.ToLower())
                return null;
            Type type = GetType();
            ExternalDataProviderInfo ext = (from r in RegisteredExternalDataProviders where r.Type == type && r.IOModeName == ExternalIOMode select r).FirstOrDefault();
            if (ext == null)
                throw new InternalError($"No external data provider for type {type.FullName} and IOMode {ExternalIOMode} found");
            return Activator.CreateInstance(ext.TypeImpl, options);
        }

        /// <summary>
        /// A collection of registered data providers.
        /// </summary>
        public static List<ExternalDataProviderInfo> RegisteredExternalDataProviders = new List<ExternalDataProviderInfo>();

        /// <summary>
        /// Registers an external data provider, typically called during startup in a class implementing the IExternalDataProvider interface.
        /// </summary>
        /// <param name="ioModeName">The I/O mode registered by the data provider (e.g, SQL, File).</param>
        /// <param name="type">The type that implements the application data provider.</param>
        /// <param name="typeImpl">The specific object type that is implemented by the low-level data provider.</param>
        /// <remarks>The RegisterExternalDataProvider registers a data provider and "connects" the application data provider with a specific implementation in a low-level data provider.</remarks>
        public static void RegisterExternalDataProvider(string ioModeName, Type type, Type typeImpl) {
            ioModeName = ioModeName.ToLower();
            ExternalDataProviderInfo ext = (from r in RegisteredExternalDataProviders where r.IOModeName == ioModeName && r.Type == type select r).FirstOrDefault();
            if (ext != null)
                throw new InternalError($"External data provider for type {type.FullName} and IOMode {ioModeName} already registered");
            RegisteredExternalDataProviders.Add(new ExternalDataProviderInfo {
                IOModeName = ioModeName,
                Type = type,
                TypeImpl = typeImpl,
            });
        }
    }
}