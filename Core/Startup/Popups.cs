/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Threading.Tasks;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Addons;

public class Popups : IAddOnSupport {

    public const int DefaultPopupWidth = 900;
    public const int DefaultPopupHeight = 600;

    public Task AddSupportAsync(YetaWFManager manager) {

        ScriptManager scripts = manager.ScriptManager;

        scripts.AddVolatileOption("Popups", "AllowPopups", manager.CurrentSite.AllowPopups);
        scripts.AddConfigOption("Popups", "DefaultPopupWidth", DefaultPopupWidth);
        scripts.AddConfigOption("Popups", "DefaultPopupHeight", DefaultPopupHeight);

        return Task.CompletedTask;
    }
}
