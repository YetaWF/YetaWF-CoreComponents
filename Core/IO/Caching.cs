/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Threading.Tasks;

namespace YetaWF.Core.IO {

    public static class Caching {
        public static ICacheObject LocalCacheProvider { get; set; }
        public static ICacheObject SharedCacheProvider { get; set; }
        public static ICacheStaticObject StaticCacheProvider { get; set; }
        /// <summary>
        /// Defines whether cache sharing is available.
        /// </summary>
        /// <remarks>Web farm/garden are only possible if shared caching is implemented (MultiInstance is true).
        /// If MultiInstance is true, cached data is shared between multiple site instances, otherwise only one instance is allowed.</remarks>
        public static bool MultiInstance { get; set; }

        public const string EmptyCachedObject = "Empty";
    };

    public class GetObjectInfo<TYPE> {
        public TYPE Data { get; set; }
        public bool Success { get; set; }
    }

    public interface ICacheObject {
        Task AddAsync<TYPE>(string key, TYPE data);
        Task<GetObjectInfo<TYPE>> GetAsync<TYPE>(string key);
        Task RemoveAsync<TYPE>(string key);
    }
    public interface ICacheStaticObject {
        Task AddAsync<TYPE>(TYPE data);
        Task<GetObjectInfo<TYPE>> GetAsync<TYPE>(Func<Task<TYPE>> noDataCallback);
        Task RemoveAsync<TYPE>();
        Task<IStaticLockObject> LockAsync<TYPE>();
    }
    public interface IStaticLockObject : IDisposable {
        Task UnlockAsync();
    }
}
