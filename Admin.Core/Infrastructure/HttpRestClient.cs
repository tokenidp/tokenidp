using Admin.Core;
using Polly;
using Polly.Extensions.Http;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Identity.Infrastructure;

public class HttpRestClient : IRestClient
{
    private string bearerToken = string.Empty;

    private readonly JsonHelper _jsonHelper;
    private readonly IAppLogger<HttpRestClient> _logger;
    private readonly HttpClient _httpClient;
    private readonly IAsyncPolicy<HttpResponseMessage> _policyWrap;

    private readonly int retryInterval = 2;

    /* HttpClient Knowledge base:
     * We have to implemented the HttpClient according to link below
     * https://docs.microsoft.com/en-us/dotnet/architecture/microservices
     * /implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests#:~
     * :text=NET%20Core%202.1%20introduced%20the,of%20delegating%20handlers%20in%20HttpClient.    
    */

    /// <summary>
    /// single param Constructor
    /// </summary>
    /// <param name="settings">Rest client default settings</param>
    public HttpRestClient(IHttpClientFactory httpClientFactory,
        JsonHelper jsonHelper,
        IAppLogger<HttpRestClient> logger)
    {
        _jsonHelper = jsonHelper;
        _logger = logger;

        _httpClient = httpClientFactory.CreateClient("PowerBIClient");

        // Define a retry policy
        var retryPolicy = Policy
             .Handle<HttpRequestException>()
             .OrResult<HttpResponseMessage>(msg => msg.StatusCode == HttpStatusCode.Unauthorized)
             //Handle 500 Internal Server Error
             .OrResult(msg => msg.StatusCode == HttpStatusCode.InternalServerError)
              //What to do if any of the above erros occur:
              //Retry 3 times, each time wait 1,2 and 4 seconds before retrying.
              .WaitAndRetryAsync(4, i => TimeSpan.FromSeconds((retryInterval * i) * 2),
              (result, timeSpan, retryCount, context) =>
              {
                  if (result.Result.StatusCode == HttpStatusCode.Unauthorized)
                  {
                      _logger.LogDebug("The Api authorization is failed with external system, " +
                          "and required login again.");
                  }

                  _logger.LogWarning($"Request failed with {result.Result.StatusCode}. " +
                      $"Waiting {timeSpan} before next retry. Retry attempt {retryCount}");
              });

        // Define a circuit breaker policy
        var circuitBreakerPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30));

        // Combine both policies into a policy wrap
        _policyWrap = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);
    }

    /// <summary>
    /// Use this service for authentication
    /// </summary>
    /// <typeparam name="T">Response</typeparam>
    /// <param name="serviceUri">Auth Uri</param>
    /// <param name="credentials">User Credentials</param>
    /// <returns></returns>
    public async Task<T> Authenticate<T>(string serviceUri,
        Dictionary<string, string> credentials)
        where T : class
    {
        var serviceRepsonse = await RetryPolicy().ExecuteAsync((()
            => AuthenticateWithRetry(serviceUri, credentials)));

        var response = CreateResponse<T>(serviceRepsonse, serviceUri);

        return response;
    }

    private async Task<HttpResponseMessage> AuthenticateWithRetry(string serviceUri,
        Dictionary<string, string> credentials)
    {
        _logger.LogTrace("The Api authenticate request is {0}.", serviceUri);

        HttpResponseMessage serviceRepsonse = default;

        SetHttpClientHeaders();

        var value = new FormUrlEncodedContent(credentials);

        serviceRepsonse = await _httpClient.PostAsync(new Uri(serviceUri), value)
              .ConfigureAwait(false);

        return serviceRepsonse;
    }

    public async Task<T> Delete<T>(string serviceUri, string token)
        where T : class
    {
        bearerToken = token;

        try
        {
            _logger.LogInfo("The Api get request is {0}.", serviceUri);

            SetHttpClientHeaders();

            var uri = new Uri(serviceUri);

            var serviceRepsonse = await _policyWrap.ExecuteAsync(() => _httpClient.DeleteAsync(uri));

            var response = CreateResponse<T>(serviceRepsonse, serviceUri);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occured when sending delete request {0}.", serviceUri);

            throw;
        }
    }

    public async Task<T> Get<T>(string serviceUri, string token)
        where T : class
    {
        bearerToken = token;
        try
        {
            _logger.LogInfo("The Api get request is {0}.", serviceUri);

            SetHttpClientHeaders();

            var uri = new Uri(serviceUri);

            var serviceRepsonse = await _policyWrap.ExecuteAsync(() => _httpClient.GetAsync(uri));

            var response = CreateResponse<T>(serviceRepsonse, serviceUri);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occured when sending get request {0}.", serviceUri);

            throw;
        }
    }

    public async Task<T> Post<T, TRequest>(string serviceUri, string token, TRequest value)
        where TRequest : class
        where T : class
    {
        bearerToken = token;
        try
        {
            _logger.LogTrace("The patch request is '{0}' & data is '{1}' ", serviceUri, value);

            SetHttpClientHeaders();

            var uri = new Uri(serviceUri);

            var serviceRepsonse = await _policyWrap.ExecuteAsync(() =>
            _httpClient.PostAsJsonAsync(_jsonHelper, uri, value));

            var response = CreateResponse<T>(serviceRepsonse, serviceUri);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occured when sending post request {0}.", serviceUri);

            throw;
        }
    }

    public async Task<T> Patch<T, TRequest>(string serviceUri, string token, TRequest value)
        where TRequest : class
        where T : class
    {
        bearerToken = token;
        try
        {
            _logger.LogTrace("The patch request is '{0}' & data is '{1}' ", serviceUri, value);

            SetHttpClientHeaders();

            var uri = new Uri(serviceUri);

            var serviceRepsonse = await _policyWrap.ExecuteAsync(() =>
            _httpClient.PatchAsJsonAsync(_jsonHelper, uri, value));

            var response = CreateResponse<T>(serviceRepsonse, serviceUri);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occured when sending patch request {0}.", serviceUri);

            throw;
        }
    }

    /// <summary>
    /// This method will create the HttpClient object, populated with default properties
    /// </summary>
    /// <param name="setTimeout">if true then override default time out</param>
    /// <param name="isRequiredAuth">if true check for authentications</param>
    /// <returns></returns>
    private void SetHttpClientHeaders()
    {
        _httpClient.DefaultRequestHeaders.Clear();

        _httpClient.DefaultRequestHeaders.Accept
                  .Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (!string.IsNullOrEmpty(bearerToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization
                   = new AuthenticationHeaderValue("Bearer", bearerToken);
        }
    }

    /// <summary>
    /// Create Rest Response
    /// </summary>
    /// <param name="message">Http Response message</param>
    /// <returns>Rest Response</returns>
    private T CreateResponse<T>(HttpResponseMessage message, string request) where T : class
    {
        if (message == null)
        {
            _logger.LogWarning("External respone is empty, and system can not deserialize the response");

            return default;
        }

        _logger.LogTrace("The API response is IsSuccess : {0}, StatusCode: {1} : JsonString: {2}",
           message.IsSuccessStatusCode, message.StatusCode, message.Content.ReadAsStringAsync().Result);

        if (message.StatusCode == HttpStatusCode.BadRequest
           || message.StatusCode == HttpStatusCode.Unauthorized
           || !message.IsSuccessStatusCode)
        {
            _logger.LogError(string.Format("The Api request is: {0} and Api error respose is: {1}",
                request, message.Content.ReadAsStringAsync().Result));

            return default;
        }

        var response = _jsonHelper.DeserializeObject<T>(message.Content.ReadAsStringAsync().Result);

        return response;
    }

    /// <summary>
    /// Set Http client Retry policy
    /// </summary>
    /// <returns>Retry policy</returns>
    private IAsyncPolicy<HttpResponseMessage> RetryPolicy()
    {
        return HttpPolicyExtensions
          // Handle HttpRequestExceptions, 408 and 5xx status codes
          .HandleTransientHttpError()
          //// Handle 404 not found
          //.OrResult(msg => msg.StatusCode == HttpStatusCode.NotFound)
          // Handle 401 Unauthorized
          .OrResult(msg => msg.StatusCode == HttpStatusCode.Unauthorized)
          //Handle 500 Internal Server Error
          .OrResult(msg => msg.StatusCode == HttpStatusCode.InternalServerError)
          //What to do if any of the above erros occur:
          //Retry 3 times, each time wait 1,2 and 4 seconds before retrying.
          .WaitAndRetryAsync(4, i => TimeSpan.FromSeconds((retryInterval * i) * 2),
          (result, timeSpan, retryCount, context) =>
           {
               if (result.Result.StatusCode == HttpStatusCode.Unauthorized)
               {
                   _logger.LogDebug("The Api authorization is failed with external system, " +
                       "and required login again.");
               }

               _logger.LogWarning($"Request failed with {result.Result.StatusCode}. " +
                   $"Waiting {timeSpan} before next retry. Retry attempt {retryCount}");
           });
    }
}
