/* Copyright © 2021 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Threading.Tasks;
using YetaWF.Core.Support;

namespace YetaWF.Core.IO {

    /// <summary>
    /// This static class represents the public accessors to caching and locking services.
    /// </summary>
    /// <remarks>
    /// The YetaWF.Core package does not implement caching and locking services.
    /// These are installed by another package during application startup.
    /// The default implementation of the caching and locking services is provided by the YetaWF.Caching package.
    /// </remarks>
    public static class Caching {

        // Data providers set by available low-level data providers during application startup

        /// <summary>
        /// A caching data provider for locally cached data (one instance of the site). Uses ASP.NET's IMemoryCache service.
        /// </summary>
        /// <remarks>
        /// This caching data provider can be used for data that needs to be cached in one single instance of a site.
        /// If multiple instances of the site are active, the cached data is NOT SHARED BETWEEN INSTANCES.
        /// </remarks>
        public static Func<ICacheDataProvider> GetLocalCacheProvider { get; set; } = null!;
        /// <summary>
        /// A caching data provider for shared cached data (between multiple instances of the site).
        /// </summary>
        /// <remarks>
        /// This caching data provider can be used for data that needs to be CACHED AND SHARED AMONG ALL INSTANCES OF A SITE.
        /// </remarks>
        public static Func<ICacheDataProvider> GetSharedCacheProvider { get; set; } = null!;
        /// <summary>
        /// A caching data provider for static shared cached data (between multiple instances of the site).
        /// </summary>
        /// <remarks>
        /// This caching data provider can be used for data that needs to be CACHED AND SHARED AMONG ALL INSTANCES OF A SITE.
        ///
        /// While similar to GetSharedCacheProvider, GetStaticCacheProvider is intended to be used in cases where data is more likely to be used repeatedly
        /// and where a single-instance site would simply use a static variable.
        /// </remarks>
        public static Func<ICacheDataProvider> GetStaticCacheProvider { get; set; } = null!;

        /// <summary>
        /// A caching data provider for locally cached SMALL data without serialization/deserialization.
        /// </summary>
        /// <remarks>
        /// This caching data provider can be used for SMALL data that needs to be cached individually in each instance of a site.
        /// If multiple instances of the site are active, the cached data is NOT SHARED BETWEEN INSTANCES.
        /// </remarks>
        public static Func<ICacheDataProvider> GetStaticSmallObjectCacheProvider { get; set; } = null!;

        /// <summary>
        /// A locking data provider for locks shared among all instances of a site.
        /// </summary>
        /// <remarks>
        /// If locking for a single instance only is required even in a multi-instance site, simply use the C# lock statement instead.
        /// </remarks>
        public static ILockProvider LockProvider { get; set; } = null!;

        /// <summary>
        /// A pub/sub provider for publish/subscribe messaging shared among all instances of a site.
        /// </summary>
        public static IPubSubProvider PubSubProvider { get; set; } = null!;
    };

    /// <summary>
    /// Interface implemented by locking data providers to lock a resource.
    /// </summary>
    public interface ILockProvider : IDisposable {
        /// <summary>
        /// Locks a resource defined by <paramref name="key"/>. The resource name is application-defined.
        /// </summary>
        /// <param name="key">The resource name.</param>
        /// <returns>Returns an lock object. This object must be disposed.</returns>
        /// <remarks>
        /// Once the returned object is disposed, the lock is released.
        /// It is better to explicitly unlock the resource using the ILockObject.UnlockAsync method due to its async nature.
        /// The object must be disposed properly in all cases, even if the ILockObject.UnlockAsync method is called to unlock.
        /// </remarks>
        Task<ILockObject> LockResourceAsync(string key);
    }
    /// <summary>
    /// The object returned by the ILockProvider.LockResourceAsync method
    /// implements this interface. It is used to unlock a locked resource.
    /// </summary>
    public interface ILockObject : IDisposable {
        /// <summary>
        /// Unlocks the resource that was locked by the call to ILockProvider.LockResourceAsync, which returned the ILockObject interface.
        /// </summary>
        Task UnlockAsync();
    }

    /// <summary>
    /// Interface implemented by pub/sub providers.
    /// </summary>
    public interface IPubSubProvider : IDisposable {
        /// <summary>
        /// Subscribe to a channel.
        /// </summary>
        /// <param name="channel">The channel name.</param>
        /// <param name="callback">The callback invoked when a message is published to the channel.</param>
        Task SubscribeAsync(string channel, Func<string, object, Task> callback);
        /// <summary>
        /// Unsubscribe from a channel.
        /// </summary>
        /// <param name="channel">The channel name.</param>
        Task UnsubscribeAsync(string channel);
        /// <summary>
        /// Publish a message to a channel.
        /// </summary>
        /// <param name="channel">The channel name.</param>
        /// <param name="message">The message object.</param>
        Task PublishAsync(string channel, object message);
    }

    /// <summary>
    /// All caching data providers implement this interface which is used to access cached data.
    /// </summary>
    public interface ICacheDataProvider : IDisposable {
        /// <summary>
        /// Adds an object to the cache.
        /// </summary>
        /// <typeparam name="TYPE">The type of the object.</typeparam>
        /// <param name="key">The resource name.</param>
        /// <param name="data">The data to cache.</param>
        Task AddAsync<TYPE>(string key, TYPE? data);
        /// <summary>
        /// Retrieves a cached object.
        /// </summary>
        /// <typeparam name="TYPE">The type of the object.</typeparam>
        /// <param name="key">The resource name.</param>
        /// <returns>Returns an object containing success indicators and the data, if available.</returns>
        Task<GetObjectInfo<TYPE>> GetAsync<TYPE>(string key);
        /// <summary>
        /// Removes the cached object.
        /// </summary>
        /// <typeparam name="TYPE"></typeparam>
        /// <param name="key">The resource name.</param>
        /// <remarks>
        /// It is permissible to remove non-existent objects.
        /// </remarks>
        Task RemoveAsync<TYPE>(string key);
    }
    /// <summary>
    /// Some caching data providers implement this interface which is used to clear all cached data.
    /// </summary>
    public interface ICacheClearable {
        /// <summary>
        /// Clears the cache completely.
        /// </summary>
        Task ClearAllAsync();
    }

    /// <summary>
    /// An instance of this class is returned by the ICacheDataProvider.GetAsync method containing success indicators and the data, if available.
    /// </summary>
    /// <typeparam name="TYPE">The type of the object.</typeparam>
    public class GetObjectInfo<TYPE> {
        /// <summary>
        /// The data. May be null if no data is available.
        /// </summary>
        public TYPE? Data { get; set; } = default!;
        /// <summary>
        /// The data. May be null if no data is available.
        /// </summary>
        public TYPE RequiredData { get { return Success ? Data! : throw new InternalError($"No data available ({nameof(Success)}=false"); } }
        /// <summary>
        /// true if retrieval was successful, false otherwise.
        /// </summary>
        public bool Success { get; set; }
    }
}
