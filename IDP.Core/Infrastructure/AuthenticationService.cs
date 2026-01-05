using IDP.Common;
using IDP.Core.Model;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace IDP.Core.Infrastructure;

public class AuthenticationService : IDisposable
{

    protected readonly HttpClient _httpClient;
    protected readonly ILogger<AuthenticationService> _logger;

    private bool _disposed;

    public AuthenticationService(
        IHttpClientFactory httpClientFactory,
        ILogger<AuthenticationService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("IDPClient");
        _logger = logger;
    }

    public async Task<AuthResponseDto> AuthenticateAsync(AuthRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "authenticate",
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = await TryReadErrorResponse(response, cancellationToken);

                _logger.LogWarning("Authentication failed for {Username}. Status: {StatusCode}, Error: {ErrorMessage}",
                    request.UserName,
                    response.StatusCode,
                    errorResponse?.Value.Error);

                throw new AuthenticationException(
                    errorResponse?.Value.Error ?? "Authentication failed",
                    (int)response.StatusCode);
            }

            var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>(
                cancellationToken: cancellationToken);

            _logger.LogInformation("Authentication successful for {Username}", request.UserName);

            return result ?? AuthResponseDto.Failure("Null response received");
        }
        catch (Exception ex) when (ex is not AuthenticationException)
        {
            _logger.LogError(ex, "Authentication error for {Username}", request.UserName);
            return AuthResponseDto.Failure($"An error occurred during authentication: {ex.Message}");
        }
    }

    protected static async Task<ApiResult<ApiError>> TryReadErrorResponse(
    HttpResponseMessage response,
    CancellationToken cancellationToken)
    {
        if (response?.Content == null)
        {
            return ApiResult<ApiError>.Failure(
                ApiError.Failure("Invalid HTTP response: No content available"));
        }

        try
        {
            var errorResult = await response.Content
                .ReadFromJsonAsync<ApiResult<ApiError>>(cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return errorResult ?? ApiResult<ApiError>.Failure(
                ApiError.Failure($"HTTP Error: {(int)response.StatusCode} - {errorResult?.Value.Error}"));
        }
        catch (JsonException jsonEx)
        {
            return ApiResult<ApiError>.Failure(
                ApiError.Failure($"Failed to parse error response: {jsonEx.Message}"));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ApiResult<ApiError>.Failure(
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