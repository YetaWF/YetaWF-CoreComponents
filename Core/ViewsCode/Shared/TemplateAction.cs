/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

namespace YetaWF.Core.Views.Shared {
    public interface ITemplateAction {
        void ExecuteAction(int action, object extrData);
    }
}
