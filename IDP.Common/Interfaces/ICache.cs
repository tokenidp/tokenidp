namespace IDP.Common.Interfaces;

public interface ICache
{
    /// <summary>
    /// Get or Create object in cache
    /// </summary>
    /// <typeparam name="T">Object Type that will get or create in cache</typeparam>
    /// <param name="key">Cache Key</param>
    /// <param name="factory">Factory method if object doesn't exist in cache</param>
    /// <param name="expiration">absolute expiration</param>
    /// <returns>cached object</returns>
    Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);

    /// <summary>
    /// add object in cache
    /// </summary>
    /// <typeparam name="T">object that will add in cache</typeparam>
    /// <param name="key">cache key</param>
    /// <param name="value">value that will add in the cache</param>
    /// <param name="expiration">absolute expiration</param>
    /// <returns></returns>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);

    /// <summary>
    /// Get cached value
    /// </summary>
    /// <typeparam name="T">object type that will return</typeparam>
    /// <param name="key">cache key</param>
    /// <returns>cached object</returns>
    Task<T?> GetAsync<T>(string key);

    /// <summary>
    /// Delete cache by key
    /// </summary>
    /// <param name="key">Cache Key</param>
    Task RemoveAsync(string key);
}