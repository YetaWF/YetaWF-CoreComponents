/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Reflection;
using System.Threading.Tasks;
using YetaWF.Core.Image;
using YetaWF.Core.Models;
using YetaWF.Core.Support;

namespace YetaWF.Core.Modules {

    public class ModuleImageSupport : IInitializeApplicationStartup {

        public const string ImageType = "YetaWF_Core_ModuleImage";

        public Task InitializeApplicationStartupAsync() {
            ImageSupport.AddHandler(ImageType, GetBytesAsync: RetrieveImageAsync);
            return Task.CompletedTask;
        }

        private async Task<ImageSupport.GetImageInBytesInfo> RetrieveImageAsync(string name, string location) {
            ImageSupport.GetImageInBytesInfo fail = new Image.ImageSupport.GetImageInBytesInfo();
            if (!string.IsNullOrWhiteSpace(location)) return fail;
            if (string.IsNullOrWhiteSpace(name)) return fail;
            string[] s = name.Split(new char[] { ',' });  // looking for "guid,propertyname"
            if (s.Length != 2) return fail;
            ModuleDefinition mod = await ModuleDefinition.LoadAsync(new Guid(s[0]), AllowNone: true);
            if (mod == null) return fail;
            Type modType = mod.GetType();
            PropertyInfo pi = ObjectSupport.TryGetProperty(modType, s[1]);
            if (pi == null) throw new InternalError("Module {0} doesn't have a property named {1}", modType.FullName, s[1]);
            byte[] content = (byte[])pi.GetValue(mod);
            return new ImageSupport.GetImageInBytesInfo {
                Content = content,
                Success = content.Length > 0,
            };
        }
    }
}
