/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using YetaWF.Core.Support;
using YetaWF.Core.Support.Serializers;
#if MVC6
using Microsoft.Extensions.Caching.Memory;
#else
#endif

namespace YetaWF.Core.IO {

    public class CachedObject {

        protected YetaWFManager Manager { get { return YetaWFManager.Manager; } }
        protected bool HaveManager { get { return YetaWFManager.HaveManager; } }

        public CachedObject() {
            Cacheable = true;
        }

        public bool Cacheable { get; set; }

        private static object EmptyCachedObject = new object();

        public void AddObjectToCache(string cacheKey, object data) {
            if (Cacheable) {
                if (data == null) {
#if MVC6
                    YetaWFManager.MemoryCache.Set<object>(cacheKey, EmptyCachedObject);
#else
                    System.Web.HttpRuntime.Cache[cacheKey] = EmptyCachedObject;
#endif

                } else {
                    // we can't save the entire object, just the data that we actually marked as saveable (Properties)
                    // the main reason the object is not saveable is because it may be derived from other classes with
                    // volatile data which is expected to be cleared for every invokation.
                    byte[] cacheData = new GeneralFormatter().Serialize(data);
#if MVC6
                    YetaWFManager.MemoryCache.Set<byte[]>(cacheKey, cacheData);
#else
                    System.Web.HttpRuntime.Cache[cacheKey] = cacheData;
#endif

                }
            }
        }

        public bool GetObjectFromCache<TYPE>(string cacheKey, out TYPE data) {
            data = default(TYPE);
            object o;
            if (!GetObjectFromCache(cacheKey, out o))
                return false;
            data = (TYPE)o;
            return true;
        }

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
        public void RemoveFromCache(string cacheKey) {
#if MVC6
            YetaWFManager.MemoryCache.Remove(cacheKey);
#else
            System.Web.HttpRuntime.Cache.Remove(cacheKey);
#endif
        }
    }
}
