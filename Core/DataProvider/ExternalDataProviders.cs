/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Linq;
using YetaWF.Core.Log;
using YetaWF.Core.Packages;
using YetaWF.Core.Support;

namespace YetaWF.Core.DataProvider {

    public interface IExternalDataProvider {
        /// <summary>
        /// Method used to register external data provider(s) implementing a specific API.
        /// </summary>
        void Register();
    }

    public class ExternalDataProviders {

        public static void RegisterExternalDataProviders() {

            Logging.AddLog($"Processing {nameof(RegisterExternalDataProviders)}");

            Logging.AddLog($"Processing {nameof(IExternalDataProvider)}");

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
            public Type Type { get; set; }
            public string IOModeName { get; set; }
            public Type TypeImpl { get; set; }
        }

        /// <summary>
        /// Creates an external assembly-based data provider.
        /// </summary>
        /// <returns>A data provider object of a type suitable for the data provider.</returns>
        protected dynamic MakeExternalDataProvider(Dictionary<string, object> options) {
            if (ExternalIOMode == NoIOMode) return null;
            Type type = GetType();
            ExternalDataProviderInfo ext = (from r in RegisteredExternalDataProviders where r.Type == type && r.IOModeName == ExternalIOMode select r).FirstOrDefault();
            if (ext == null)
                throw new InternalError($"No external data provider for type {type.FullName} and IOMode {ExternalIOMode} found");
            return Activator.CreateInstance(ext.TypeImpl, options);
        }

        public static List<ExternalDataProviderInfo> RegisteredExternalDataProviders = new List<ExternalDataProviderInfo>();

        /// <summary>
        /// Registers an external data provider, typically called during startup in a class implementing the IExternalDataProvider interface.
        /// </summary>
        /// <param name="type">The implemented data provider type.</param>
        /// <param name="getDP">A method that will create a data provider of the requested type.</param>
        public static void RegisterExternalDataProvider(string ioModeName, Type type, Type typeImpl) {
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