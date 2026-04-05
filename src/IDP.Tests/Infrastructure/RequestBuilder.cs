using System.Text;

namespace IDP.Tests.Infrastructure;

public class RequestBuilder
{
    private readonly HttpClient _httpClient;
    private string _route = string.Empty;
    private readonly QueryParamBuilder _queryParamBuilder = new();

    public RequestBuilder(HttpClient client)
    {
        _httpClient = client;
        _httpClient.BaseAddress = new Uri("https://localhost:44302/");
    }

    public RequestBuilder AddRoute(string route)
    {
        _route = route;

        return this;
    }

    public RequestBuilder AddQueryParams(string key, string value)
    {
        _queryParamBuilder.Add(key, value);
        return this;
    }

    private static StringContent ToStringContent<T>(T content) where T : class
    {
        return new StringContent(JsonUtils.Serialize(content), Encoding.UTF8, "application/json");
    }

    private static StringContent ToStringContent(string content)
    {
        return new StringContent(content, Encoding.UTF8, "application/json");
    }

    private string BuildUrl() => $"{_route}{_queryParamBuilder.Build()}";

    public async Task<HttpResponseMessage> GetResponse(string acceptHeader = "text/json")
    {
        var client = _httpClient;
        client.DefaultRequestHeaders.Add("Accept", acceptHeader);
        return await client.GetAsync(BuildUrl());
    }

    private static void ThrowIfError(HttpResponseMessage message)
    {
        if (!message.IsSuccessStatusCode)
        {
            var responseContent = message.Content.ReadAsStringAsync().Result;

            throw new HttpRequestException($"Could not make {message.RequestMessage?.Method.ToString()
                ?? "UNKNOWN METHOD"} Request.\n{responseContent}");
        }
    }

    public async Task<HttpResponseMessage> Get(bool throwOnError = true)
    {
        var response = await GetResponse();

        if (throwOnError) ThrowIfError(response);
        return response;
    }

    public async Task<HttpResponseMessage> Post<T>(T content, bool throwOnError = true) where T : class
    {
        _httpClient.DefaultRequestHeaders.Add("TransactionId", Guid.NewGuid().ToString());
        var response = await _httpClient.PostAsync(BuildUrl(), ToStringContent(content));

        if (throwOnError) ThrowIfError(response);
        return response;
    }

    public async Task<HttpResponseMessage> Post(bool throwOnError = true)
    {
        var response = await _httpClient.PostAsync(BuildUrl(), ToStringContent("{}"));
        if (throwOnError) ThrowIfError(response);
        return response;
    }

    public async Task<HttpResponseMessage> Put<T>(T content, bool throwOnError = true) where T : class
    {
        var response = await _httpClient.PutAsync(BuildUrl(), ToStringContent(content));
        if (throwOnError) ThrowIfError(response);
        return response;
    }

    public async Task<HttpResponseMessage> Delete(bool throwOnError = true)
    {
        var response = await _httpClient.DeleteAsync(BuildUrl());
        if (throwOnError) ThrowIfError(response);
        return response;
    }
}