/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;

namespace YetaWF.Core.Support {

    /// <summary>
    /// Manages permanently available items in-memory in a single site instance.
    /// Used for locally stored data.
    /// </summary>
    /// <remarks>Permanent items are available until the site is restarted and are shared within one site of a YetaWF instance.
    ///
    /// Permanent items are identified by their Type. Only one item of a specific Type can be saved for one site. Each site within a YetaWF instance can save the same Type once.
    ///
    /// This is typically used for permanent, high use data which should be cached and is shared across one site of a YetaWF instance.
    /// Cannot be used with distributed caching, i.e., data the must be shared with other instances.</remarks>
    public static class PermanentManager {

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }
        private static bool HaveManager { get { return YetaWFManager.HaveManager; } }

        private class ObjectDictionary : Dictionary<string, object> { }
        private static ObjectDictionary _objDictionary = new ObjectDictionary();

        /// <summary>
        /// Clears all permanent items.
        /// </summary>
        public static void ClearAll() {
            _objDictionary = new ObjectDictionary();
        }
        /// <summary>
        /// Adds a new permanent item.
        /// </summary>
        /// <typeparam name="TObj">The Type of the item.</typeparam>
        /// <param name="obj">The item object.</param>
        /// <remarks>If this item has already been added, it is replaced instead.
        ///
        /// To avoid race conditions, the caller must use a locking mechanism.
        /// </remarks>
        public static void AddObject<TObj>(TObj obj) {
            PermanentManager.RemoveObject<TObj>();
            int identity = (HaveManager && Manager.HaveCurrentSite) ? Manager.CurrentSite.Identity : 0;
            _objDictionary.Add(identity + "/" + typeof(TObj).FullName, obj);
        }
        /// <summary>
        /// Retrieves a permanent item.
        /// </summary>
        /// <typeparam name="TObj">The Type of the item.</typeparam>
        /// <param name="obj">Returns the item object. null is returned if the item doesn't exist.</param>
        /// <returns>Returns true if successful, false otherwise.</returns>
        public static bool TryGetObject<TObj>(out TObj obj) {
            obj = default(TObj);
            object tempObj;
            int identity = (HaveManager && Manager.HaveCurrentSite) ? Manager.CurrentSite.Identity : 0;
            if (!_objDictionary.TryGetValue(identity + "/" + typeof(TObj).FullName, out tempObj))
                return false;
            obj = (TObj) tempObj;
            return true;
        }
        /// <summary>
        /// Retrieves a permanent item.
        /// </summary>
        /// <typeparam name="TObj">The Type of the item.</typeparam>
        /// <returns>Returns the item object.</returns>
        public static TObj GetObject<TObj>() {
            TObj obj;
            if (!TryGetObject<TObj>(out obj))
                throw new InternalError("Couldn't retrieve permanent object of type {0} for host {1}", typeof(TObj).FullName, Manager.CurrentSite.Identity);
            return obj;
        }
        /// <summary>
        /// Removes a permanent item.
        /// </summary>
        /// <typeparam name="TObj">The Type of the item.</typeparam>
        public static void RemoveObject<TObj>() {
            int identity = (HaveManager && Manager.HaveCurrentSite) ? Manager.CurrentSite.Identity : 0;
            _objDictionary.Remove(identity + "/" + typeof(TObj).FullName);
        }
    }
}
