namespace Identity.Application.Common.Interfaces;

public interface ICache
{
    /// <summary>
    /// add object in cache
    /// </summary>
    /// <typeparam name="T">object that will add in cache</typeparam>
    /// <param name="key">cache key</param>
    /// <param name="value">>value that will add in the cache</param>
    /// <param name="slidingExpiration">set sliding cache expiry time</param>
    void Add<T>(string key, T value, TimeSpan? expiration = null) where T : class;

    /// <summary>
    /// Get single object from memory
    /// </summary>
    /// <typeparam name="T">object that will return</typeparam>
    /// <param name="key">cache key</param>
    /// <returns>object</returns>
    T GetValue<T>(string key) where T : class;

    /// <summary>
    /// Get or create async cache
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <param name="generatorasync"></param>
    /// <returns></returns>
    Task<T> GetValueAsync<T>(string key, Func<Task<T>> task) where T : class;

    /// <summary>
    /// Get list of objects from memory
    /// </summary>
    /// <typeparam name="T">object that will return</typeparam>
    /// <param name="key">cache key</param>
    /// <returns>list of objects</returns>
    IEnumerable<T> GetList<T>(string key) where T : class;

    /// <summary>
    /// Delete cache by key
    /// </summary>
    /// <param name="key">Cache Key</param>
    void Delete(string key);
}
