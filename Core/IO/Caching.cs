﻿/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Threading.Tasks;

namespace YetaWF.Core.IO {

    public static class Caching {

        // Dataproviders set by available data providers during application startup
        public static ICacheObject LocalCacheProvider { get; set; }
        public static ICacheObject SharedCacheProvider { get; set; }
        public static ICacheStaticObject StaticCacheProvider { get; set; }
        
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
        Task AddAsync<TYPE>(string key, TYPE data);
        Task<TYPE> GetAsync<TYPE>(string key, Func<Task<TYPE>> noDataCallback = null);
        Task RemoveAsync<TYPE>(string key);
        Task<IStaticLockObject> LockAsync<TYPE>(string key);
    }
    public interface IStaticLockObject : IDisposable {
        Task UnlockAsync();
    }
}
