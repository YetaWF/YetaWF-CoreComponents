/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using YetaWF.Core.Support;

namespace YetaWF.Core.Addons.Templates {
    public class Image : IAddOnSupport {

        public const string FormatUrl = "/File.image?Type={0}&Location={1}&Name={2}"; // Url for an image
        public const string FormatUrlWithSize = "/File.image?Type={0}&Location={1}&Name={2}&Width={3}&Height={4}&Stretch={5}"; // Url for an image (resized to fit)
        public const string FormatUrlForceHttpHandler = "/FileHndlr.image?Type={0}&Location={1}&Name={2}"; // Url for an image
        public const string FormatUrlWithSizeForceHttpHandler = "/FileHndlr.image?Type={0}&Location={1}&Name={2}&Width={3}&Height={4}&Stretch={5}"; // Url for an image (resized to fit)

        public void AddSupport(YetaWFManager manager) { }
    }
}
