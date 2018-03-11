/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Threading.Tasks;
using YetaWF.Core.Support;

namespace YetaWF.Core.Addons.Templates {
    public class Image : IAddOnSupport {

        public const string FormatUrl = "/File.image?Type={0}&Location={1}&Name={2}"; // Url for an image
        public const string FormatUrlWithSize = "/File.image?Type={0}&Location={1}&Name={2}&Width={3}&Height={4}&Stretch={5}"; // Url for an image (resized to fit)
        public const string FormatUrlForceHttpHandler = "/FileHndlr.image?Type={0}&Location={1}&Name={2}"; // Url for an image
        public const string FormatUrlWithSizeForceHttpHandler = "/FileHndlr.image?Type={0}&Location={1}&Name={2}&Width={3}&Height={4}&Stretch={5}"; // Url for an image (resized to fit)

        public Task AddSupportAsync(YetaWFManager manager) {
            return Task.CompletedTask;
        }
    }
}
