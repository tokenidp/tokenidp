using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Services.Common.Interfaces;
using Services.Common.Model;

namespace Services.Common;

public sealed class MemoryCache : ICache
{
    private readonly IMemoryCache _cache;
    private readonly IAppLogger<MemoryCache> _logger;
    private readonly TimeSpan defaultExpiration;

    public MemoryCache(IMemoryCache cache,
        IOptions<ConfigurationSetting> options,
        IAppLogger<MemoryCache> logger)
    {
        _cache = cache;
        defaultExpiration = new(0, options.Value.DefautlCacheExpiryInMinutes, 0);
        _logger = logger;
    }

    /// <summary>
    /// add object in cache
    /// </summary>
    /// <typeparam name="T">object that will add in cache</typeparam>
    /// <param name="key">cache key</param>
    /// <param name="value">>value that will add in cache</param>
    /// <param name="slidingExpiration">set sliding cache expiry time</param>
    public void Add<T>(string key, T value, TimeSpan? expiration = null) where T : class
    {
        Delete(key);

        // Set cache options.
        var options = new MemoryCacheEntryOptions()
        {
            // Keep in cache for this time
            AbsoluteExpirationRelativeToNow = expiration ?? defaultExpiration,
        };

        options.RegisterPostEvictionCallback(callback: EvictionCallback, state: this);

        _cache.Set(key, value, options);
    }

    /// <summary>
    /// Get single object from memory
    /// </summary>
    /// <typeparam name="T">object that will return</typeparam>
    /// <param name="key">cache key</param>
    /// <returns>object</returns>
    public T GetValue<T>(string key) where T : class
    {
        return _cache.Get<T>(key);
    }

    /// <summary>
    /// Get list of objects from memory
    /// </summary>
    /// <typeparam name="T">object that will return</typeparam>
    /// <param name="key">cache key</param>
    /// <returns>list of objects</returns>
    public IEnumerable<T> GetList<T>(string key) where T : class
    {
        return _cache.Get<IEnumerable<T>>(key);
    }

    /// <summary>
    /// Get or create async cache
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <param name="generatorasync"></param>
    /// <returns></returns>
    public async Task<T> GetValueAsync<T>(string key, Func<Task<T>> task) where T : class
    {
        var cacheEntry = await
          _cache.GetOrCreateAsync<T>(key, async entry =>
          {
              entry.SlidingExpiration = TimeSpan.FromSeconds(15 * 60);
              return await task();
          });

        return cacheEntry;
    }

    /// <summary>
    /// Delete cache by key
    /// </summary>
    /// <param name="key">Cache Key</param>
    public void Delete(string key)
    {
        _cache.Remove(key);
    }

    /// <summary>
    /// Thie would trigger when cache expire
    /// </summary>
    /// <param name="key">Cache Key</param>
    /// <param name="value">Cache Value</param>
    /// <param name="reason">Eviction Reason</param>
    /// <param name="state">Current object state</param>
    private void EvictionCallback(
        object key,
        object value,
        EvictionReason reason,
        object state)
    {
        _logger.LogDebug("Cache has been expired for the {0} - Reason : {1}", key, reason);
    }
}
