/* Copyright © 2021 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Threading.Tasks;
using YetaWF.Core.Image;
using YetaWF.Core.IO;

namespace YetaWF.Core.Support.Image {

    public class FileImageSupport : IInitializeApplicationStartup {

        // IInitializeApplicationStartup
        // IInitializeApplicationStartup
        // IInitializeApplicationStartup

        public const string ImageType = "YetaWF_Core_File";

        public Task InitializeApplicationStartupAsync() {
            YetaWF.Core.Image.ImageSupport.AddHandler(ImageType, GetBytesAsync: RetrieveImageAsync);
            return Task.CompletedTask;
        }

        private async Task<ImageSupport.GetImageInBytesInfo> RetrieveImageAsync(string? name, string? location) {
            ImageSupport.GetImageInBytesInfo fail = new ImageSupport.GetImageInBytesInfo();
            if (!string.IsNullOrWhiteSpace(location)) return fail;
            if (string.IsNullOrWhiteSpace(name)) return fail;

            if (!name.StartsWith(Globals.VaultUrl) && !name.StartsWith(Globals.VaultPrivateUrl)) // only allow vault files, otherwise this would be a huge security hole
                return fail;
            string file = Utility.UrlToPhysical(name);

            if (YetaWFManager.DiagnosticsMode) {
                if (!await FileSystem.FileSystemProvider.FileExistsAsync(file))
                    return fail;
            }

            try {
                return new ImageSupport.GetImageInBytesInfo() {
                    Content = await FileSystem.FileSystemProvider.ReadAllBytesAsync(file),
                    Success = true,
                };
            } catch (Exception) {
                return fail;
	        }
        }
    }
}
