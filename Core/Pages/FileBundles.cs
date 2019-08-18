/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YetaWF.Core.IO;
using YetaWF.Core.Log;
using YetaWF.Core.Serializers;
using YetaWF.Core.Support;

namespace YetaWF.Core.Pages {

    public class FileBundles : IInitializeApplicationStartupFirstNodeOnly {

        public enum BundleTypeEnum {
            JS = 0,
            CSS = 1,
        }

        public class Bundle {
            public string BundleName { get; set; }
            public int BundleNumber { get; set; }
            public string Url { get; set; }
            public int StartLength { get; set; }
#if DEBUG
            public string StartText { get; set; }
            // The generated text added to the start of the file
            // There is a chance this text differs between identical included js/css files, so this helps us debug that condition
#endif
        }

        /// <summary>
        /// Called when the first node of a multi-instance site is starting up.
        /// </summary>
        public async Task InitializeFirstNodeStartupAsync() {
            // delete all files from last session and recreate the folder
            if (YetaWFManager.Manager.HostUsed != YetaWFManager.BATCHMODE && YetaWFManager.Manager.HostUsed != YetaWFManager.SERVICEMODE) {
                Logging.AddLog("Removing/creating bundle folder");
                string tempPath = Path.Combine(YetaWFManager.RootFolder, Globals.AddonsBundlesFolder);
                if (await FileSystem.TempFileSystemProvider.DirectoryExistsAsync(tempPath))
                    await FileSystem.TempFileSystemProvider.DeleteDirectoryAsync(tempPath);
            }
        }

        public static async Task<string> MakeBundleAsync(List<string> fileList, BundleTypeEnum bundleType, ScriptBuilder startText = null) {

            string url = null;

            if (fileList.Count > 0) {

                string start = startText != null ? startText.ToString() : "";
                int startLength = start.Length;

                string extension;
                switch (bundleType) {
                    default:
                    case BundleTypeEnum.JS:
                        extension = ".js";
                        break;
                    case BundleTypeEnum.CSS:
                        extension = ".css";
                        break;
                }

                string bundleName = MakeName(fileList);

                string BUNDLEKEY = $"__FileBundles_{YetaWFManager.Manager.CurrentSite.Identity}";

                using (ILockObject bundleLock = await YetaWF.Core.IO.Caching.LockProvider.LockResourceAsync(BUNDLEKEY)) {
                    using (ICacheDataProvider cacheStaticDP = YetaWF.Core.IO.Caching.GetStaticCacheProvider()) {
                        SerializableList<Bundle> bundles;
                        GetObjectInfo<SerializableList<Bundle>> info = await cacheStaticDP.GetAsync<SerializableList<Bundle>>(BUNDLEKEY);
                        if (info.Success)
                            bundles = info.Data;
                        else
                            bundles = new SerializableList<Bundle>();
                        Bundle bundle = (from b in bundles where b.BundleName == bundleName select b).FirstOrDefault();
                        if (bundle == null || startLength != bundle.StartLength) {
                            // make a new temp file combining all files in the list
                            StringBuilder sb = new StringBuilder();
                            if (!string.IsNullOrWhiteSpace(start))
                                sb.Append(start);
#if DEBUG
                            sb.Append("/* File Map: ----------------------------------------\n");
                            long total = 0;
                            foreach (string file in fileList) {
                                long len = new FileInfo(Utility.UrlToPhysical(file)).Length;
                                total += len;
                                sb.Append($"{len:##,#}\t{file}\n");
                            }
                            sb.Append($"{total:##,#}\tTotal\n");
                            sb.Append("--------------------------------------------------*/\n");
#endif
                            foreach (var file in fileList) {
                                string fileText = await FileSystem.FileSystemProvider.ReadAllTextAsync(Utility.UrlToPhysical(file));
                                if (!string.IsNullOrWhiteSpace(fileText)) {
#if DEBUG
                                    sb.AppendFormat("/**** {0} ****/\n", file);
#endif
                                    if (bundleType == BundleTypeEnum.CSS)
                                        fileText = ProcessIncludedFiles(fileText, file);
                                    sb.Append(fileText);
                                    sb.Append("\n");
                                }
                            }
                            if (bundle != null) {
                                // new bundle, different start length
                                // new generated javascript (most likely caused by different user authorizations)
                                int existingBundleNumber = bundle.BundleNumber;
                                bundle = new Bundle {
                                    BundleName = bundleName,
                                    BundleNumber = existingBundleNumber,
                                    Url = GetBundleUrlName(existingBundleNumber, startLength, extension),
                                    StartLength = startLength,
#if DEBUG
                                    StartText = start,
#endif
                                };
                            } else {
                                // new bundle
                                bundle = new Bundle {
                                    BundleName = bundleName,
                                    BundleNumber = bundles.Count,
                                    Url = GetBundleUrlName(bundles.Count, startLength, extension),
                                    StartLength = startLength,
#if DEBUG
                                    StartText = start,
#endif
                                };
                            }
                            bundles.Add(bundle);
                            string realFile = Utility.UrlToPhysical(bundle.Url);
                            await FileSystem.TempFileSystemProvider.CreateDirectoryAsync(Path.GetDirectoryName(realFile));
                            await FileSystem.TempFileSystemProvider.WriteAllTextAsync(realFile, sb.ToString());
                            await cacheStaticDP.AddAsync(BUNDLEKEY, bundles);
                        } else {
                            // existing bundle
#if DEBUG
                            if (start != bundle.StartText)
                                throw new InternalError("Investigate! Same length start text but different contents");
#endif
                        }
                        url = bundle.Url;
                        await bundleLock.UnlockAsync();
                    }
                }
            }
            return url;
        }

        private static string GetBundleUrlName(int index, int startLength, string extension) {
            return string.Format("/{0}/{1}/bundle_{2}_{3}{4}", Globals.AddonsBundlesFolder, YetaWFManager.Manager.CurrentSite.Identity, index, startLength, extension);
        }

        private static readonly Regex varUrlRegex = new Regex("(?'pre'[ :\\,]url\\()(?'path'[^\\)]+)", RegexOptions.Compiled | RegexOptions.Multiline);

        /// <summary>
        /// Process all url() definitions so we access the files in the correct location (the bundle is in a different location)
        /// </summary>
        public static string ProcessIncludedFiles(string fileText, string file) {
            // replace all instances of url( with the correct path
            int ix = file.LastIndexOf('/');
            if (ix < 0) throw new InternalError($"{file} is not a url");
            string path = file.Substring(0, ix);
            fileText = varUrlRegex.Replace(fileText, (match) => ProcessMatch(match, path));
            return fileText;
        }
        private static string ProcessMatch(Match match, string filePath) {
            string pre = match.Groups["pre"].Value;
            string path = match.Groups["path"].Value;
            string pathStart = path.TrimStart();
            if (pathStart.StartsWith("/") || pathStart.StartsWith("http") || pathStart.StartsWith("https"))
                return string.Format($"{pre}{path}");
            if (pathStart.StartsWith("\"data:"))
                return string.Format($"{pre}{path}");
            if (pathStart.StartsWith("'") || pathStart.StartsWith("\""))
                return string.Format("{0}{1}{2}/{3}", pre, pathStart.Substring(0, 1), filePath, pathStart.Substring(1));
            else
                return string.Format($"{pre}{filePath}/{path}");
        }

        /// <summary>
        /// Make a key name based on the file list
        /// </summary>
        /// <param name="fileList"></param>
        /// <returns></returns>
        private static string MakeName(List<string> fileList) {
            string name = "";

            List<string> list = (from l in fileList orderby l select l.ToLower()).ToList();
            foreach (var f in list)
                name += f + ",";
            return name;
        }
    }
}
