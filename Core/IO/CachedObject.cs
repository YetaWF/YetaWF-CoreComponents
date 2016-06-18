/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using YetaWF.Core.Support;
using YetaWF.Core.Support.Serializers;

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
            if (!HaveManager || !Manager.HaveCurrentContext)
                return;
            if (Cacheable) {
                if (data == null)
                    Manager.CurrentContext.Cache[cacheKey] = EmptyCachedObject;
                else {
                    // we can't save the entire object, just the data that we actually marked as saveable (Properties)
                    // the main reason the object is not saveable is because it may be derived from other classes with
                    // volatile data which is expected to be cleared for every invokation.
                    Manager.CurrentContext.Cache[cacheKey] = new GeneralFormatter().Serialize(data);
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
            if (!HaveManager || !Manager.HaveCurrentContext)
                return false;
            data = Manager.CurrentContext.Cache[cacheKey];
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
            if (!HaveManager || !Manager.HaveCurrentContext)
                return;
            Manager.CurrentContext.Cache.Remove(cacheKey);
        }

    }
}
