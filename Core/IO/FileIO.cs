/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.IO;
using YetaWF.Core.Support;
using YetaWF.Core.Support.Serializers;

namespace YetaWF.Core.IO {

    /// <summary>
    /// Implements file I/O for an object of type TObj.
    /// </summary>
    public class FileIO<TObj> {

        public FileIO() {
#if DEBUG
            Format = GeneralFormatter.Style.Simple; // the preferred format - change for debugging if desired
#else
            Format = GeneralFormatter.Style.Simple;
#endif
        }

        public string BaseFolder { get; set; } // The full path of the folder where the file(s) is/are stored
        public string FileName { get; set; }
        public DateTime? Date { get; set; } // file save/load date
        public object Data { get; set; } // the data saved/loaded

        public GeneralFormatter.Style Format { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        protected string FullPath {
            get {
                if (string.IsNullOrEmpty(BaseFolder)) throw new InternalError("BaseFolder is empty");
                // sanitize the file name and translate invalid characters
                string file = FileData.MakeValidFileName(FileName);
                return string.IsNullOrEmpty(FileName) ? BaseFolder : Path.Combine(BaseFolder, file);
            }
        }
        protected string LockName {
            get {
                return "YetaWF##Lock##" + FullPath.ToLower();
            }
        }

        /// <summary>
        /// Loads the file.
        /// </summary>
        /// <returns></returns>
        public TObj Load() {
            if (string.IsNullOrEmpty(BaseFolder)) throw new InternalError("BaseFolder is empty");

            try {
                Date = System.IO.File.GetLastWriteTimeUtc(FullPath);
            } catch (Exception) { }
            if (typeof(TObj) == typeof(string)) {
                try {
                    Data = System.IO.File.ReadAllText(FullPath);
                } catch (Exception) { }
            } else {
                FileStream fs;
#if DEBUG
                if (!File.Exists(FullPath))
                    return default(TObj);
#endif
                try {
                    fs = new FileStream(FullPath, FileMode.Open);
                } catch (Exception exc) {
                    if (!(exc is FileNotFoundException || exc is DirectoryNotFoundException)) throw;
                    return default(TObj);
                }
                byte[] btes = new byte[fs.Length];
                fs.Read(btes, 0, (int) fs.Length);
                fs.Close();
                Data = new GeneralFormatter(Format).Deserialize(btes);
            }
            return (TObj) Data;
        }

        /// <summary>
        /// Saves the file.
        /// </summary>
        /// <param name="replace"></param>
        /// <returns></returns>
        public bool Save(bool replace = true) {
            if (string.IsNullOrEmpty(BaseFolder)) throw new InternalError("BaseFolder is empty");
            if (Data == null) throw new InternalError("no data");

            Directory.CreateDirectory(BaseFolder);

            if (!replace && File.Exists(FullPath))
                return false;

            if (typeof(TObj) == typeof(string)) {
                System.IO.File.WriteAllText(FullPath, (string) Data);
            } else {
                FileStream fs = new FileStream(FullPath, FileMode.Create);
                new GeneralFormatter(Format).Serialize(fs, Data);
                fs.Close();
            }
            if (Date != null)
                System.IO.File.SetLastWriteTime(FullPath, ((DateTime)Date).ToLocalTime() );
            return true;
        }

        /// <summary>
        /// Removes the file.
        /// Throws an error if the file does not exist.
        /// </summary>
        public void Remove() {
            if (string.IsNullOrEmpty(BaseFolder)) throw new InternalError("BaseFolder is empty");
            File.Delete(FullPath);
        }
        /// <summary>
        /// Removes the file.
        /// Ignores any errors.
        /// </summary>
        public bool TryRemove() {
            if (string.IsNullOrEmpty(BaseFolder)) throw new InternalError("BaseFolder is empty");
            bool success = false;
            if (File.Exists(FullPath)) {
                File.Delete(FullPath);
                success = true;
            }
            return success;
        }

        /// <summary>
        /// Test whether the file exists.
        /// </summary>
        public bool Exists() {
            if (string.IsNullOrEmpty(BaseFolder)) throw new InternalError("BaseFolder is empty");
            return File.Exists(FullPath);
        }
    }
}
