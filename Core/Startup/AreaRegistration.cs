/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using YetaWF.Core.Packages;

namespace YetaWF.Core.Controllers {
    public class AreaRegistration : YetaWF.Core.Controllers.AreaRegistrationBase {
        public AreaRegistration() : base() {
            CurrentPackage = this.GetCurrentPackage();
        }
        public static Package CurrentPackage;
    }
}
