/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

#nullable enable

namespace YetaWF.Core.Support {
    public interface ITemplateAction {
        bool ExecuteAction(int action, bool modelIsValid, object extrData);
    }
}
