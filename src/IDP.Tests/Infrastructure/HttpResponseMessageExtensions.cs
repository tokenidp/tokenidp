namespace IDP.Tests.Infrastructure;

public static class HttpResponseMessageExtensions
{
    public static Task<T?> DeserializeContent<T>(this HttpResponseMessage message)
    {
        return JsonUtils.DeserializeAsync<T>(message.Content.ReadAsStreamAsync());
    }
}