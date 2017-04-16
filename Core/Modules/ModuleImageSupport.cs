/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Reflection;
using YetaWF.Core.Image;
using YetaWF.Core.Models;
using YetaWF.Core.Support;

namespace YetaWF.Core.Modules {

    public class ModuleImageSupport : IInitializeApplicationStartup {

        // IInitializeApplicationStartup
        // IInitializeApplicationStartup
        // IInitializeApplicationStartup

        public const string ImageType = "YetaWF_Core_ModuleImage";

        public void InitializeApplicationStartup() {
            ImageSupport.AddHandler(ImageType, GetBytes: RetrieveImage);
        }

        private bool RetrieveImage(string name, string location, out byte[] content) {
            content = null;
            if (!string.IsNullOrWhiteSpace(location)) return false;
            if (string.IsNullOrWhiteSpace(name)) return false;
            string[] s = name.Split(new char[] { ',' });  // looking for "guid,propertyname"
            if (s.Length != 2) return false;
            ModuleDefinition mod = ModuleDefinition.Load(new System.Guid(s[0]), AllowNone: true);
            if (mod == null) return false;
            Type modType = mod.GetType();
            PropertyInfo pi = ObjectSupport.TryGetProperty(modType, s[1]);
            if (pi == null) throw new InternalError("Module {0} doesn't have a property named {1}", modType.FullName, s[1]);
            content = (byte[]) pi.GetValue(mod);
            return content.Length > 0;
        }
    }
}
