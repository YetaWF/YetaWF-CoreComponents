/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using YetaWF.Core.DataProvider.Attributes;
using YetaWF.Core.Models;
using YetaWF.Core.Models.Attributes;

namespace YetaWF.Core.Modules {

    // Designed modules - LoadDesignedModules returns a site-specific list
    
    public class DesignedModule {
        [Data_PrimaryKey]
        public Guid ModuleGuid { get; set; }
        public string Name { get; set; }
        public MultiString Description { get; set; }

        public DesignedModule() {
            Description = new MultiString();
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable", Justification = "Not used for serialization")]
    public class DesignedModulesDictionary : Dictionary<Guid, DesignedModule> { }

    public class DesignedModules {

        // this must be provided by a dataprovider during app startup (this loads module information)
        [DontSave]
        public static Func<List<DesignedModule>> LoadDesignedModules { get; set; }

    }
}
