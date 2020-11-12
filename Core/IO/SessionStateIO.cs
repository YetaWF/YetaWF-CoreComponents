/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using YetaWF.Core.Support;
using YetaWF.Core.Support.Serializers;

namespace YetaWF.Core.IO {

    /// <summary>
    /// Implements SessionState I/O for an object of type TObj.
    /// Used to save/load object to/from .NET (Core) session state.
    /// </summary>
    /// <remarks>This can be used by applications to store session information. This is intended for large data objects.
    ///
    /// For smaller data objects, the class YetaWF.Core.Support.Repository.SettingsDictionary offers storing dictionaries with named objects in session state.
    /// </remarks>
    public class SessionStateIO<TObj> {

        private YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        /// <summary>
        /// The name of the object to load/save in session state.
        /// </summary>
        public string Key { get; set; }
        /// <summary>
        /// The object to save or loaded from session state.
        /// </summary>
        public object Data { get; set; } // the data saved/loaded

        /// <summary>
        /// Loads the object named by the Key property.
        /// </summary>
        /// <returns>Returns the object.</returns>
        public TObj Load() {
            if (!Manager.HaveCurrentSession) return default(TObj);
            byte[] data;
            data = Manager.CurrentSession.GetBytes(Key);
            if (data == null) return default(TObj);
            Data = new GeneralFormatter(GeneralFormatter.Style.Simple).Deserialize<TObj>(data);
            return (TObj) Data;
        }

        /// <summary>
        /// Saves the object defined by the Data property with the named Key.
        /// </summary>
        public void Save() {
            if (Data == null) throw new InternalError("No data");
            if (!Manager.HaveCurrentSession) throw new InternalError("No session");
            Manager.CurrentSession.SetBytes(Key, new GeneralFormatter(GeneralFormatter.Style.Simple).Serialize(Data));
        }

        /// <summary>
        /// Removes the object named by the Key property.
        /// Throws an error if the object does not exist.
        /// </summary>
        public void Remove() {
            if (!Manager.HaveCurrentSession) throw new InternalError("No session");
            Manager.CurrentSession.Remove(Key);
        }
    }
}
