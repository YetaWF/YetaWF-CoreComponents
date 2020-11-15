/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YetaWF.Core.DataProvider;
using YetaWF.Core.Modules;

namespace YetaWF.Core.IO {

    /// <summary>
    /// This class provides access to the module data provider.
    /// The module data provider sets all access methods (like LoadModuleDefinitionAsync) during application startup.
    ///
    /// These methods should never be called by applications. They are intended for framework use only.
    /// Use the methods provided by YetaWF.Core.Modules.ModuleDefinition instead.
    /// </summary>
    /// <remarks>
    /// All properties must be provided by a module data provider during application startup.
    /// The properties in this class provide access to the module data provider. A module data provider must be accessed through members of this class ONLY.
    /// </remarks>
    public static class Module {

        /// <summary>
        /// Loads a module given the module Guid and returns the module object. It can be cast to a more specific derived type.
        /// </summary>
        public static Func<Guid, Task<ModuleDefinition>> LoadModuleDefinitionAsync { get; set; } = null!;
        /// <summary>
        /// Saves a module.
        /// </summary>
        public static Func<ModuleDefinition, IModuleDefinitionIO, Task> SaveModuleDefinitionAsync { get; set; } = null!;
        /// <summary>
        /// Removes a module given the module Guid. Returns true if the module was removed, false if the module doesn't exist. All other errors cause an exception.
        /// </summary>
        public static Func<Guid, Task<bool>> RemoveModuleDefinitionAsync { get; set; } = null!;
        /// <summary>
        /// Locks a module given the module Guid.
        /// </summary>
        public static Func<Guid, Task<ILockObject>> LockModuleAsync { get; set; } = null!;
        /// <summary>
        /// Retrieves a collection of modules given sort/filter/paging criteria.
        /// </summary>
        public static Func<ModuleBrowseInfo, Task> GetModulesAsync { get; set; } = null!;

        /// <summary>
        /// An instance of this class is used as parameter to the GetModulesAsync method to provide sort/filter/paging criteria
        /// and returns the collection of modules.
        /// </summary>
        public class ModuleBrowseInfo {
            /// <summary>
            /// The number of records to skip.
            /// </summary>
            public int Skip { get; set; }
            /// <summary>
            /// The number of records to retrieve. If more records are available they are dropped.
            /// </summary>
            public int Take { get; set; }
            /// <summary>
            /// A collection describing the sort order.
            /// </summary>
            public List<DataProviderSortInfo>? Sort { get; set; }
            /// <summary>
            /// A collection describing the filtering criteria.
            /// </summary>
            public List<DataProviderFilterInfo>? Filters { get; set; }
            /// <summary>
            /// The total number of records matching the filtering criteria, without considering paging.
            /// </summary>
            public int Total { get; set; }
            /// <summary>
            /// The collection of records matching the filtering criteria and limited to the requested number of records (Skip/Take).
            /// </summary>
            public List<ModuleDefinition>? Modules { get; set; }
        }
    };
}
