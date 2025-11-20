using IDP.Web.Model;
using System.Text.Json;

namespace IDP.Web.Authentication;

public class BaseService<TService> : IDisposable
{
    protected readonly HttpClient _httpClient;
    protected readonly ILogger<TService> _logger;

    private bool _disposed;

    public BaseService(
         IHttpClientFactory httpClientFactory,
         ILogger<TService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("IDPClient");
        _logger = logger;
    }

    protected static async Task<Result<ApiError>> TryReadErrorResponse(
    HttpResponseMessage response,
    CancellationToken cancellationToken)
    {
        if (response?.Content == null)
        {
            return Result<ApiError>.Failure(
                ApiError.Failure("Invalid HTTP response: No content available"));
        }

        try
        {
            var errorResult = await response.Content
                .ReadFromJsonAsync<Result<ApiError>>(cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return errorResult ?? Result<ApiError>.Failure(
                ApiError.Failure($"HTTP Error: {(int)response.StatusCode} - {errorResult?.Value.Error}"));
        }
        catch (JsonException jsonEx)
        {
            return Result<ApiError>.Failure(
                ApiError.Failure($"Failed to parse error response: {jsonEx.Message}"));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Result<ApiError>.Failure(
                ApiError.Failure($"HTTP Error: {(int)(response?.StatusCode ?? 0)} - {response?.ReasonPhrase ?? "Unknown Error"}"));
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _httpClient?.Dispose();
            _logger.LogInformation("AuthenticationService disposed");
        }

        _disposed = true;
    }
}
