/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;
using System.IO;
using YetaWF.Core.Log;
using YetaWF.Core.Support;

namespace YetaWF.Core.Packages {

    public partial class Package {

        public static void UpdateAll() {

            // create models for each package
            foreach (Package package in Package.GetAvailablePackages()) {
                Logging.AddLog("Creating/updating {0}", package.Name);
                List<string> errorList = new List<string>();
                if (!package.InstallModels(errorList)) {
                    ScriptBuilder sb = new ScriptBuilder();
                    sb.Append(__ResStr("cantInstallModels", "Can't install models for package {0}:(+nl)"), package.Name);
                    sb.Append(errorList, LeadingNL: true);
                    Logging.AddErrorLog("Failure in {0}", package.Name, errorList);
                    throw new InternalError(sb.ToString());
                }
            }
            File.Delete(Path.Combine(YetaWFManager.RootFolder, Globals.UpdateIndicatorFile));
        }
    }
}
