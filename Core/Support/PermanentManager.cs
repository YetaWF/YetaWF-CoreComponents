/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;

namespace YetaWF.Core.Support {

    /// <summary>
    /// Manages permanently available items. 
    /// </summary>
    public static class PermanentManager {

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        private class ObjectDictionary : Dictionary<string, object> { }
        private static ObjectDictionary _objDictionary = new ObjectDictionary();

        public static void ClearAll() {
            _objDictionary = new ObjectDictionary();
        }

        public static void AddObject<TObj>(TObj obj) {
            PermanentManager.RemoveObject<TObj>();
            int identity = Manager.HaveCurrentSite ? Manager.CurrentSite.Identity : 0;
            _objDictionary.Add(identity + "/" + typeof(TObj).FullName, obj);
        }
        public static bool TryGetObject<TObj>(out TObj obj) {
            obj = default(TObj);
            object tempObj;
            int identity = Manager.HaveCurrentSite ? Manager.CurrentSite.Identity : 0;
            if (!_objDictionary.TryGetValue(identity + "/" + typeof(TObj).FullName, out tempObj))
                return false;
            obj = (TObj) tempObj;
            return true;
        }
        public static TObj GetObject<TObj>() {
            TObj obj;
            if (!TryGetObject<TObj>(out obj))
                throw new InternalError("Couldn't retrieve permanent object of type {0} for host {1}", typeof(TObj).FullName, Manager.CurrentSite.Identity);
            return obj;
        }
        public static void RemoveObject<TObj>() {
            int identity = Manager.HaveCurrentSite ? Manager.CurrentSite.Identity : 0;
            _objDictionary.Remove(identity + "/" + typeof(TObj).FullName);
        }
    }
}
