/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using YetaWF.Core.Packages;

namespace YetaWF.Core.Controllers {
    public partial class AreaRegistration {
        public AreaRegistration() : this(out CurrentPackage) { }
        public static Package CurrentPackage;
    }
}
