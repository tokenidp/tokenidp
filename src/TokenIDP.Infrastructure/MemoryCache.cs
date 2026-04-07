using Microsoft.Extensions.Caching.Memory;
using TokenIDP.Core.Abstractions;

namespace TokenIDP.Infrastructure;

internal sealed class MemoryCache : ICache
{
    private readonly IMemoryCache _cache;
    private readonly IAppLogger<MemoryCache> _logger;
    private readonly TimeSpan defaultExpiration;

    public MemoryCache(IMemoryCache cache,
        IAppLogger<MemoryCache> logger)
    {
        _cache = cache;
        defaultExpiration = new(0, 30, 0);
        _logger = logger;
    }

    public Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
    {
        _logger.LogTrace("Getting or creating cache entry for key {Key}", key);

        return _cache.GetOrCreateAsync(key, async entry =>
        {
            _logger.LogDebug("Cache miss for key {Key}. Creating new entry.", key);

            if (expiration.HasValue)
            {
                _logger.LogDebug("Setting expiration for key {Key} to {Expiration}", key, expiration);

                entry.AbsoluteExpirationRelativeToNow = expiration;
            }

            entry.RegisterPostEvictionCallback(callback: EvictionCallback, state: this);
            _logger.LogTrace("Registered eviction callback for key {Key}", key);

            try
            {
                var value = await factory();
                _logger.LogTrace("Successfully created value for key {Key}", key);
                return value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating value for cache key {Key}", key);
                throw;
            }
        });
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        _logger.LogTrace("Setting cache entry for key {Key}", key);

        var options = new MemoryCacheEntryOptions()
        {
            // Keep in cache for this time
            AbsoluteExpirationRelativeToNow = expiration ?? defaultExpiration,
        };

        _logger.LogDebug("Setting cache entry for key {Key} with expiration {Expiration}",
            key, expiration?.ToString() ?? "default");


        options.RegisterPostEvictionCallback(callback: EvictionCallback, state: this);
        _logger.LogTrace("Registered eviction callback for key {Key}", key);

        try
        {
            _cache.Set(key, value, options);
            _logger.LogTrace("Successfully set cache entry for key {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache entry for key {Key}", key);
            throw;
        }

        return Task.CompletedTask;
    }

    public Task<T?> GetAsync<T>(string key)
    {
        _logger.LogTrace("Attempting to get cache entry for key {Key}", key);

        bool found = _cache.TryGetValue(key, out T? value);

        if (found)
        {
            _logger.LogDebug("Cache hit for key {Key}", key);
            _logger.LogTrace("Returning cached value for key {Key}", key);
        }
        else
        {
            _logger.LogDebug("Cache miss for key {Key}", key);
        }

        return Task.FromResult(value);
    }

    public Task RemoveAsync(string key)
    {
        _logger.LogTrace("Attempting to remove cache entry for key {Key}", key);

        try
        {
            _cache.Remove(key);
            _logger.LogDebug("Successfully removed cache entry for key {Key}", key);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache entry for key {Key}", key);
            throw;
        }
    }

    /// <summary>
    /// Thie would trigger when cache expire
    /// </summary>
    /// <param name="key">Cache Key</param>
    /// <param name="value">Cache Value</param>
    /// <param name="reason">Eviction Reason</param>
    /// <param name="state">Current object state</param>
    private void EvictionCallback(object key,
        object value,
        EvictionReason reason,
        object state)
    {
        _logger.LogDebug("Cache entry evicted. Key: {Key}, Reason: {EvictionReason}",
            key.ToString(),
            reason.ToString());

        switch (reason)
        {
            case EvictionReason.Expired:
                _logger.LogTrace("Cache entry naturally expired for key {Key}", key);
                break;
            case EvictionReason.Removed:
                _logger.LogTrace("Cache entry actively removed for key {Key}", key);
                break;
            case EvictionReason.Replaced:
                _logger.LogTrace("Cache entry replaced for key {Key}", key);
                break;
            case EvictionReason.TokenExpired:
                _logger.LogTrace("Cache entry evicted due to token expiration for key {Key}", key);
                break;
            case EvictionReason.Capacity:
                _logger.LogWarning("Cache entry evicted due to capacity pressure for key {Key}", key);
                break;
        }
    }
}
