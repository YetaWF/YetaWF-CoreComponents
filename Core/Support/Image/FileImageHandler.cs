using System.IO;

namespace YetaWF.Core.Support.Image {

    public class FileImageSupport : IInitializeApplicationStartup {

        // IInitializeApplicationStartup
        // IInitializeApplicationStartup
        // IInitializeApplicationStartup

        public const string ImageType = "YetaWF_Core_File";

        public void InitializeApplicationStartup() {
            YetaWF.Core.Image.ImageSupport.AddHandler(ImageType, GetBytes: RetrieveImage);
        }

        private bool RetrieveImage(string name, string location, out byte[] content) {
            content = null;
            if (!string.IsNullOrWhiteSpace(location)) return false;
            if (string.IsNullOrWhiteSpace(name)) return false;

            if (!name.StartsWith(Globals.VaultUrl) && !name.StartsWith(Globals.VaultPrivateUrl)) // only allow vault files, otherwise this would be a huge security hole
                return false;
            string file = YetaWFManager.UrlToPhysical(name);
            if (!File.Exists(file))
                return false;
            content = File.ReadAllBytes(file);
            return true;
        }
    }
}
