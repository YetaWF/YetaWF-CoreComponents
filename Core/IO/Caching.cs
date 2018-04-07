/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Threading.Tasks;

namespace YetaWF.Core.IO {

    public static class Caching {

        // Dataproviders set by available data providers during application startup
        public static Func<ICacheDataProvider> GetLocalCacheProvider { get; set; }
        public static Func<ICacheDataProvider> GetSharedCacheProvider { get; set; }
        public static Func<ICacheDataProvider> GetStaticCacheProvider { get; set; }

        public static ILockProvider LockProvider { get; set; }

        public const string EmptyCachedObject = "Empty";
    };

    public interface ILockProvider : IDisposable {
        Task<ILockObject> LockResourceAsync(string key);
    }
    public interface ILockObject : IDisposable {
        Task UnlockAsync();
    }
    public interface ICacheDataProvider : IDisposable {
        Task AddAsync<TYPE>(string key, TYPE data);
        Task<GetObjectInfo<TYPE>> GetAsync<TYPE>(string key);
        Task RemoveAsync<TYPE>(string key);
    }
    public class GetObjectInfo<TYPE> {
        public TYPE Data { get; set; }
        public bool Success { get; set; }
    }
}
