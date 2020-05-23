/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

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

        public static string GetResourceString(Type type, string name, string defaultValue, params object[] args) {

            string text;
            if (LocalizationSupport.UseLocalizationResources) {
                string fullName = type.FullName;
                //if (...) {
                //    // use the base class to store resources
                //    type = type.BaseType;
                //}
                fullName = type.FullName.Split(new char[] { '`' }).First(); // chop off any generics <>
                LocalizationData locData = Localization.Load(Package.GetPackageFromAssembly(type.Assembly), fullName, Localization.Location.Merge);
                if (locData != null) {
                    text = locData.FindString(name);
                    if (text == null) {
                        if (type.BaseType != null && type.BaseType.FullName == "YetaWF.Core.Modules.ModuleDefinition") {
                            // shared views use the module base class
                            return GetResourceString(type.BaseType, name, defaultValue, args);
                        }
                        if (LocalizationSupport.AbortOnFailure)
                            throw new InternalError("Missing resource {0} for class {1}", name, type.FullName);
#if DEBUG
                        text = $"{defaultValue}(*missing* {name})";
#else
                        text = defaultValue;
#endif
                    }
                } else {
                    text = defaultValue;
                    if (YetaWFManager.HaveManager && YetaWFManager.Manager.LocalizationSupportEnabled) {
                        SiteDefinition site = YetaWFManager.Manager.CurrentSite;
                        if (site != null && site.Localization) {
                            if (LocalizationSupport.AbortOnFailure)
                                throw new InternalError("Missing resource class file {0}", type.FullName);
#if DEBUG
                            text = $"{defaultValue}(*class missing* {name})";
#endif
                        }
                    }
                }
            } else
                text = defaultValue;

            if (args != null && args.Count() > 0)
                return string.Format(text, args);
            else
                return text;
        }
    }
}
