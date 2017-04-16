/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */


#if MVC6
    // dropped
#else

using System.Web;

namespace YetaWF.Core.Support {

    public static class BrowserCaps {

        public static bool SupportedBrowser(HttpBrowserCapabilities caps) {

            // some of these cutoffs are arbitrary - if the browser is more than 3 years old, we'll return "unsupported"
            // this is mainly done for IE - supporting anything below 9 is pointless
            switch (caps.Browser.ToLower()) {
                case "safari":
                    //if (caps.MajorVersion < 7) return false;
                    break;
                case "chrome":
                    //if (caps.MajorVersion < 35) return false;
                    break;
                case "firefox":
                    //if (caps.MajorVersion < 30) return false;
                    break;
                case "ie":
                case "internetexplorer":
                    // we really only care about IE
                    if (caps.MajorVersion < 9) return false;
                    break;
                default:
                    // whatevz
                    break;
            }
            return true;
        }
    }
}

#endif
