namespace Identity.Application.Common.Interfaces;

/// <summary>
/// RESTful Service client
/// </summary>
public interface IRestClient
{
    /// <summary>
    /// Use this service for authentication
    /// </summary>
    /// <typeparam name="T">Response</typeparam>
    /// <param name="serviceUri">Auth Uri</param>
    /// <param name="credentials">User Credentials</param>
    /// <returns></returns>
    Task<T> Authenticate<T>(string serviceUri, Dictionary<string, string> credentials)
        where T : class;

    /// <summary>
    /// Get object by id
    /// </summary>
    /// <typeparam name="T">object that will be returned</typeparam>
    /// <param name="request">request resource uri</param>
    /// <param name="id">object id</param>
    /// <returns>Generic RestResponse object</returns>
    Task<T> Delete<T>(string serviceUri, string token) where T : class;

    /// <summary>
    /// Get list of objects
    /// </summary>
    /// <param name="request">request resource uri</param>
    /// <returns></returns>
    Task<T> Get<T>(string serviceUri, string token) where T : class;

    /// <summary>
    /// Post a response
    /// </summary>
    /// <typeparam name="TResponse"></typeparam>
    /// <typeparam name="T"></typeparam>
    /// <param name="request"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    Task<T> Post<T, TRequest>(string serviceUri, string token, TRequest value)
       where TRequest : class
       where T : class;

    /// <summary>
    /// post a partial object update
    /// </summary>
    /// <typeparam name="TResponse"></typeparam>
    /// <typeparam name="T"></typeparam>
    /// <param name="request"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    Task<T> Patch<T, TRequest>(string serviceUri, string token, TRequest value)
       where TRequest : class
       where T : class;
}
