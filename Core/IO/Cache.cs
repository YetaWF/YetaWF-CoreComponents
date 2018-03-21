/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Threading.Tasks;

namespace YetaWF.Core.IO {

    public static class Caching {
        public static ICacheObject LocalCacheProvider { get; set; }
        public static ICacheObject SharedCacheProvider { get; set; }

        public const string EmptyCachedObject = "Empty";
    };

    public class GetObjectInfo<TYPE> {
        public TYPE Data { get; set; }
        public bool Success { get; set; }
    }

    public interface ICacheObject {
        Task AddAsync<TYPE>(string key, TYPE data);
        Task<GetObjectInfo<TYPE>> GetAsync<TYPE>(string key);
        Task RemoveAsync(string key);
    }
}
