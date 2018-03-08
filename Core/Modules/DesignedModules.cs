/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YetaWF.Core.Models;

namespace YetaWF.Core.Modules {

    /// <summary>
    /// Describes a designed module.
    /// </summary>
    public class DesignedModule {
        /// <summary>
        /// The module Guid.
        /// </summary>
        public Guid ModuleGuid { get; set; }
        /// <summary>
        /// The module name as provided by user who created the module.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The module description, provided by module implementer.
        /// </summary>
        public MultiString Description { get; set; }
        /// <summary>
        /// The area name implementing the module.
        /// </summary>
        public string AreaName { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public DesignedModule() {
            Description = new MultiString();
        }
    }

    /// <summary>
    /// Cached designed modules.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable", Justification = "Not used for serialization")]
    public class DesignedModulesDictionary : Dictionary<Guid, DesignedModule> { }

    /// <summary>
    /// Designed modules management.
    /// </summary>
    public static class DesignedModules {

        /// <summary>
        /// Loads and caches all designed modules.
        /// </summary>
        /// <remarks>This method is implemented by a data provider, set at application startup.</remarks>
        public static Func<Task<List<DesignedModule>>> LoadDesignedModulesAsync { get; set; }
    }
}
