/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Linq;
using YetaWF.Core.IO;
using YetaWF.Core.Packages;
using YetaWF.Core.Site;
using YetaWF.Core.Support;

namespace YetaWF.Core.Localize {

    public static class ResourceAccessHelper {
        // access resources from instantiated class
        public static string __ResStr(this object obj, string name, string defaultValue, params object[] parms) {
            return ResourceAccess.GetResourceString(obj.GetType(), name, defaultValue, parms);
        }
        // access resources from static class
        // helper function to be added to static class
        //private static string __ResStr(string name, string defaultValue, params object[] parms) { return xResourceAccess.GetResourceString(typeof(yourstaticClass), name, defaultValue, parms); }
    }

    public static class ResourceAccess {

        // RETRIEVAL
        // RETRIEVAL
        // RETRIEVAL

        public static string GetResourceString(Type type, string name, string defaultValue, params object[] args)
        {
            if (LocalizationSupport.UseLocalizationResources) {
                string fullName = type.FullName;
                //if (...) {
                //    // use the base class to store resources
                //    type = type.BaseType;
                //}
                fullName = type.FullName.Split(new char[] { '`' }).First(); // chop off any generics <>
                string text;
                LocalizationData locData = Localization.Load(Package.GetPackageFromAssembly(type.Assembly), fullName, Localization.Location.Merge);
                if (locData != null) {
                    text = locData.FindString(name);
                    if (text != null) {
                        if (args != null && args.Count() > 0)
                            text = string.Format(text, args);
                    } else {
                        if (type.BaseType != null && type.BaseType.FullName == "YetaWF.Core.Modules.ModuleDefinition") {
                            // shared views use the module base class
                            return GetResourceString(type.BaseType, name, defaultValue, args);
                        }
                        text = "*miss*" + name;
                        if (LocalizationSupport.AbortOnFailure)
                            throw new InternalError("Missing resource {0} for class {1}", name, type.FullName);
                    }
                    return text;
                } else {
                    if (YetaWFManager.HaveManager && YetaWFManager.Manager.LocalizationSupportEnabled) {
                        SiteDefinition site = YetaWFManager.Manager.CurrentSite;
                        if (site != null && site.Localization) {
                            text = "*clsmiss*" + name;
                            if (LocalizationSupport.AbortOnFailure)
                                throw new InternalError("Missing resource class file {0}", type.FullName);
                            return text;
                        }
                    }
                }
            }
            if (args != null && args.Count() > 0)
                defaultValue = string.Format(defaultValue, args);
            return defaultValue;
        }
    }
}
