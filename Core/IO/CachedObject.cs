/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using YetaWF.Core.Support;
using YetaWF.Core.Support.Serializers;
#if MVC6
using Microsoft.Extensions.Caching.Memory;
#else
#endif

namespace YetaWF.Core.IO {

    /// <summary>
    /// Handles in memory cache, local to current process, managing objects as key/value pair.
    /// </summary>
    /// <remarks>The actual implementation is provided by an external replaceable data provider.</remarks>
    public class CachedObject {

        protected YetaWFManager Manager { get { return YetaWFManager.Manager; } }
        protected bool HaveManager { get { return YetaWFManager.HaveManager; } }

        public CachedObject() {
            Cacheable = true;
        }

        /// <summary>
        /// Defines whether the object should be saved in cache.
        /// </summary>
        public bool Cacheable { get; set; }

        private static object EmptyCachedObject = new object();

        /// <summary>
        /// Adds object to in-process cache using the specified key.
        /// </summary>
        /// <param name="cacheKey">The key used to save the object in cache.</param>
        /// <param name="data">The data to save in cache.</param>
        public void AddObjectToCache(string cacheKey, object data) {
            if (Cacheable) {
                if (data == null) {
#if MVC6
                    YetaWFManager.MemoryCache.Set<object>(cacheKey, EmptyCachedObject);
#else
                    System.Web.HttpRuntime.Cache[cacheKey] = EmptyCachedObject;
#endif
                } else {
                    // we can't save the entire object, just the data that we actually marked as savable (Properties)
                    // the main reason the object is not savable is because it may be derived from other classes with
                    // volatile data which is expected to be cleared for every invocation.
                    byte[] cacheData = new GeneralFormatter().Serialize(data);
#if MVC6
                    YetaWFManager.MemoryCache.Set<byte[]>(cacheKey, cacheData);
#else
                    System.Web.HttpRuntime.Cache[cacheKey] = cacheData;
#endif
                }
            }
        }

        /// <summary>
        /// Retrieve an object from in-process cache.
        /// </summary>
        /// <typeparam name="TYPE">The type of the object to retrieve.</typeparam>
        /// <param name="cacheKey">The key used to retrieve the object from cache.</param>
        /// <param name="data">The data retrieved from cache or the type's default value (e.g., null).</param>
        /// <returns>true if the object was in cache, false otherwise.</returns>
        public bool GetObjectFromCache<TYPE>(string cacheKey, out TYPE data) {
            data = default(TYPE);
            object o;
            if (!GetObjectFromCache(cacheKey, out o))
                return false;
            data = (TYPE)o;
            return true;
        }
        /// <summary>
        /// Retrieve an object from in-process cache.
        /// </summary>
        /// <param name="cacheKey">The key used to retrieve the object from cache.</param>
        /// <param name="data">The data retrieved from cache or null.</param>
        /// <returns>true if the object was in cache, false otherwise.</returns>
        public bool GetObjectFromCache(string cacheKey, out object data) {
            data = null;
#if MVC6
            data = YetaWFManager.MemoryCache.Get(cacheKey);
#else
            data = System.Web.HttpRuntime.Cache[cacheKey];
#endif

            if (data == EmptyCachedObject) {
                data = null;
                return true;
            } else if (data != null) {
                data = new GeneralFormatter().Deserialize((byte[])data);
                return true;
            } else
                return false;
        }
        /// <summary>
        /// Removes an object from in-process cache.
        /// </summary>
        /// <param name="cacheKey">The key of the object to remove from cache.</param>
        /// <remarks>It is not an error to remove a non-existent object.</remarks>
        public void RemoveFromCache(string cacheKey) {
#if MVC6
            YetaWFManager.MemoryCache.Remove(cacheKey);
#else
            System.Web.HttpRuntime.Cache.Remove(cacheKey);
#endif
        }
    }
}
