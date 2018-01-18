/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using YetaWF.Core.Support;
using YetaWF.Core.Support.Serializers;

namespace YetaWF.Core.IO {

    /// <summary>
    /// Implements SessionState I/O for an object of type TObj.
    /// </summary>
    public class SessionStateIO<TObj> {

        protected YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        public string Key { get; set; }
        public object Data { get; set; } // the data saved/loaded

        /// <summary>
        /// Loads the data.
        /// </summary>
        /// <returns></returns>
        public TObj Load() {
            if (!Manager.HaveCurrentSession) return default(TObj);
            byte[] data;
            data = Manager.CurrentSession.GetBytes(Key);
            if (data == null) return default(TObj);
            Data = new GeneralFormatter(GeneralFormatter.Style.Simple).Deserialize(data);
            return (TObj) (object) Data;
        }

        /// <summary>
        /// Saves the file.
        /// </summary>
        public void Save() {
            if (Data == null) throw new InternalError("No data");
            if (!Manager.HaveCurrentSession) throw new InternalError("No session");
            Manager.CurrentSession.SetBytes(Key, new GeneralFormatter(GeneralFormatter.Style.Simple).Serialize(Data));
        }

        /// <summary>
        /// Removes the file.
        /// Throws an error if the file does not exist.
        /// </summary>
        public void Remove() {
            if (!Manager.HaveCurrentSession) throw new InternalError("No session");
            Manager.CurrentSession.Remove(Key);
        }
    }
}
