/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.IO;

namespace YetaWF.Core.IO {
    public static class DirectoryIO {

        public static void DeleteFolder(string targetFolder) {
            if (!Directory.Exists(targetFolder)) return;// avoid exception spam

            int retry = 10; // folder occasionally are in use to we'll just wait a bit
            while (retry > 0) {
                try {
                    Directory.Delete(targetFolder, true);
                    return;
                } catch (Exception exc) {
                    if (exc is DirectoryNotFoundException)
                        return;// done
                    if (retry <= 1)
                        throw;
                }
                System.Threading.Thread.Sleep(1000); // wait a bit
                --retry;
            }
        }

        public static void CreateFolder(string targetFolder) {
            if (Directory.Exists(targetFolder)) return;// avoid exception spam

            int retry = 10; // folder occasionally are in use to we'll just wait a bit
            while (retry > 0) {
                try {
                    Directory.CreateDirectory(targetFolder);
                    return;
                } catch (Exception) {
                    if (retry <= 1)
                        throw;
                }
                System.Threading.Thread.Sleep(500); // wait a bit
                --retry;
            }
        }
    }
}
