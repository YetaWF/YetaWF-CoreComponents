using System.IO;
using System.Threading.Tasks;
using YetaWF.Core.Image;

namespace YetaWF.Core.Support.Image {

    public class FileImageSupport : IInitializeApplicationStartup {

        // IInitializeApplicationStartup
        // IInitializeApplicationStartup
        // IInitializeApplicationStartup

        public const string ImageType = "YetaWF_Core_File";

        public Task InitializeApplicationStartupAsync(bool firstNode) {
            YetaWF.Core.Image.ImageSupport.AddHandler(ImageType, GetBytesAsync: RetrieveImageAsync);
            return Task.CompletedTask;
        }

        private Task<ImageSupport.GetImageInBytesInfo> RetrieveImageAsync(string name, string location) {
            Task<ImageSupport.GetImageInBytesInfo> fail = Task.FromResult(new ImageSupport.GetImageInBytesInfo());
            if (!string.IsNullOrWhiteSpace(location)) return fail;
            if (string.IsNullOrWhiteSpace(name)) return fail;

            if (!name.StartsWith(Globals.VaultUrl) && !name.StartsWith(Globals.VaultPrivateUrl)) // only allow vault files, otherwise this would be a huge security hole
                return fail;
            string file = YetaWFManager.UrlToPhysical(name);
            if (!File.Exists(file))
                return fail;
            return Task.FromResult(new ImageSupport.GetImageInBytesInfo() {
                Content = File.ReadAllBytes(file),
                Success = true,
            });
        }
    }
}
