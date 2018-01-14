/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
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

        public class ExternalDataProviderInfo {
            public Func<object, dynamic> NewFunc { get; set; }
        }

        /// <summary>
        /// Creates an external assembly-based data provider.
        /// </summary>
        /// <returns>A data provider object of a type suitable for the data provider.</returns>
        protected dynamic MakeExternalDataProvider(object options) {
            Type requestedType = GetType();
            ExternalDataProviderInfo info;
            if (!RegisteredExternalDataProviders.TryGetValue(GetType(), out info))
                throw new InternalError($"No registered external data provider for type {requestedType.FullName}");
            return info.NewFunc(options);
        }

        private static Dictionary<Type, ExternalDataProviderInfo> RegisteredExternalDataProviders = new Dictionary<Type, ExternalDataProviderInfo>();

        /// <summary>
        /// Registers an external data provider, typically called during startup in a class implementing the IExternalDataProvider interface.
        /// </summary>
        /// <param name="type">The implemented data provider type.</param>
        /// <param name="getDP">A method that will create a data provider of the requested type.</param>
        public static void RegisterExternalDataProvider(Type type, Func<object, dynamic> getDP) {
            if (RegisteredExternalDataProviders.ContainsKey(type)) throw new InternalError($"Data provider for type {type.FullName} already registered");
            RegisteredExternalDataProviders.Add(type, new ExternalDataProviderInfo {
                NewFunc = getDP,
            });
        }
    }
}