/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

namespace YetaWF.Core.Views.Shared {
    public interface ITemplateAction {
        void ExecuteAction(int action, object extrData);
    }
}
