using System.Net.Http;
using System.Net.Http.Headers;

namespace Identity.Infrastructure;

public static class HttpClientExtension
{
    public static Task<HttpResponseMessage> PostAsJsonAsync<T>(
        this HttpClient httpClient,
        JsonHelper jsonHelper,
        Uri url,
        T data)
    {
        var dataAsString = jsonHelper.SerializeObject(data);
        var content = new StringContent(dataAsString);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        return httpClient.PostAsync(url, content);
    }

    public static Task<HttpResponseMessage> PatchAsJsonAsync<T>(
        this HttpClient httpClient,
        JsonHelper jsonHelper,
        Uri url,
        T data)
    {
        var dataAsString = jsonHelper.SerializeObject(data);
        var content = new StringContent(dataAsString);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        return httpClient.PatchAsync(url, content);
    }

    public static async Task<T> ReadAsJsonAsync<T>(
        this HttpContent content,
        JsonHelper jsonHelper)
    {
        var dataAsString = await content.ReadAsStringAsync();
        return jsonHelper.DeserializeObject<T>(dataAsString);
    }
}
