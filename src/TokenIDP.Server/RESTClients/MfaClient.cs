using TokenIDP.Core.OAuth.Model;
using TokenIDP.Core.Foundation;
using TokenIDP.Core.Foundation.Abstractions;
using TokenIDP.Core.Foundation.Contracts;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace TokenIDP.Server.RESTClients;

public class MfaClient : IDisposable
{
    protected readonly HttpClient _httpClient;
    protected readonly IAppLogger<MfaClient> _logger;

    private bool _disposed;

    public MfaClient(IHttpClientFactory httpClientFactory, IAppLogger<MfaClient> logger)
    {
        _httpClient = httpClientFactory.CreateClient("IDPClient");
        _logger = logger;
    }

    public async Task<AuthorizationResponse> ResendMfaCodeAsync(MfaRequest request,
       CancellationToken cancellationToken = default)
    {
        try
        {
            if (request == null)
            {
                return AuthorizationResponse.Failure("Request object cannot be empty.");
            }

            using var content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            using var response = await _httpClient.PostAsync(
                "mfa/resend",
                content,
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(30);

                return AuthorizationResponse.Failure($"Too many attempts. Please try again after {retryAfter.TotalSeconds} seconds.");
            }

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AuthorizationResponse>(
                    cancellationToken: cancellationToken);

                return result ?? AuthorizationResponse.Failure("Received empty response from MFA service");
            }

            var error = await TryReadErrorResponse(response, cancellationToken);

            _logger.LogWarning("Resend Mfa failed for {Username}. Status: {StatusCode}, Error: {ErrorMessage}",
                    error?.Value?.CorrelationId ?? "invalid CorrelationId",
                    response.StatusCode,
                    error?.Value?.Error ?? "invalid request");

            throw new AuthenticationException(
                error?.Value?.Error ?? "Resend Mfa failed",
                (int)response.StatusCode);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError(ex, "Resend MFA was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Resend MFA failed");
            return AuthorizationResponse.Failure($"Resend MFA failed: {ex.Message}");
        }
    }

    public async Task<AuthorizationResponse> VerifyMfaAsync(MfaRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request == null)
            {
                return AuthorizationResponse.Failure("Request object cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(request.Code) || request.Code.Length != 6)
            {
                return AuthorizationResponse.Failure("Invalid MFA code format. Must be 6 digits.");
            }

            using var content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            using var response = await _httpClient.PostAsync(
                "mfa/verify",
                content,
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(30);

                return AuthorizationResponse.Failure($"Too many attempts. Please try again after {retryAfter.TotalSeconds} seconds.");
            }

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AuthorizationResponse>(
                    cancellationToken: cancellationToken);

                return result ?? AuthorizationResponse.Failure("Received empty response from MFA service");
            }

            var error = await TryReadErrorResponse(response, cancellationToken);

            _logger.LogWarning("Authentication failed for {Username}. Status: {StatusCode}, Error: {ErrorMessage}",
                    error?.Value?.CorrelationId ?? "invalid CorrelationId",
                    response.StatusCode,
                    error?.Value?.Error ?? "invalid request");

            throw new AuthenticationException(
                error?.Value?.Error ?? "Authentication failed",
                (int)response.StatusCode);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError(ex, "MFA verification was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MFA verification failed");
            return AuthorizationResponse.Failure($"MFA verification failed: {ex.Message}");
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
            _logger.LogInfo("AuthenticationService disposed");
        }

        _disposed = true;
    }
}
