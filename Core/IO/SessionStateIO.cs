/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

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
            byte[] data = (Manager.CurrentSession[Key] as byte[]);//"foreign" session data can be non-byte[]
            if (data == null) return default(TObj);
            data = (byte[])Manager.CurrentSession[Key];
            if (data == null) return default(TObj);

            Data = new GeneralFormatter().Deserialize(data);
            return (TObj) (object) Data;
        }

        /// <summary>
        /// Saves the file.
        /// </summary>
        public void Save() {
            if (Data == null) throw new InternalError("No data");
            if (!Manager.HaveCurrentSession) throw new InternalError("No session");
            Manager.CurrentSession[Key] = new GeneralFormatter().Serialize(Data);
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
