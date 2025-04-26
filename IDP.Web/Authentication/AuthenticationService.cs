using IDP.Web.Model;
using System.Net;
using System.Text;
using System.Text.Json;

namespace IDP.Web.Authentication;

public class AuthenticationService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthenticationService> _logger;
    private bool _disposed;

    public AuthenticationService(
        IHttpClientFactory httpClientFactory,
        ILogger<AuthenticationService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("IDPClient");
        _logger = logger;
    }

    public async Task<Result<AuthResponse>> AuthenticateAsync(AuthRequest request,
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

            var result = await response.Content.ReadFromJsonAsync<Result<AuthResponse>>(
                cancellationToken: cancellationToken);

            _logger.LogInformation("Authentication successful for {Username}", request.UserName);

            return result ?? Result<AuthResponse>.Failure(
                   AuthResponse.Failure("Null response received"));
        }
        catch (Exception ex) when (ex is not AuthenticationException)
        {
            _logger.LogError(ex, "Authentication error for {Username}", request.UserName);
            return Result<AuthResponse>.Failure(
                   AuthResponse.Failure($"An error occurred during authentication: {ex.Message}"));
        }
    }

    public async Task<Result<AuthResponse>> ResendMfaCodeAsync(MfaRequest request,
       CancellationToken cancellationToken = default)
    {
        try
        {
            if (request == null)
            {
                return Result<AuthResponse>
                                    .Failure(AuthResponse.Failure("Request object cannot be empty."));
            }

            using var content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            using var response = await _httpClient.PostAsync(
                "authenticate/resend-mfa",
                content,
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(30);

                return Result<AuthResponse>.Failure(
                    AuthResponse.Failure($"Too many attempts. Please try again after {retryAfter.TotalSeconds} seconds."));
            }

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<Result<AuthResponse>>(
                    cancellationToken: cancellationToken);

                return result ?? Result<AuthResponse>.Failure(
                    AuthResponse.Failure("Received empty response from MFA service"));
            }

            var error = await TryReadErrorResponse(response, cancellationToken);
            _logger.LogWarning("Resend Mfa failed for {Username}. Status: {StatusCode}, Error: {ErrorMessage}",
                    error.Value.CorrelationId,
                    response.StatusCode,
                    error.Value.Error);

            throw new AuthenticationException(
                error.Value.Error ?? "Resend Mfa failed",
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
            return Result<AuthResponse>.Failure(
                    AuthResponse.Failure($"Resend MFA failed: {ex.Message}"));
        }
    }

    public async Task<Result<AuthResponse>> VerifyMfaAsync(MfaRequest request, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request == null)
            {
                return Result<AuthResponse>
                                    .Failure(AuthResponse.Failure("Request object cannot be empty."));
            }

            if (string.IsNullOrWhiteSpace(request.Code) || request.Code.Length != 6)
            {
                return Result<AuthResponse>.Failure(
                    AuthResponse.Failure("Invalid MFA code format. Must be 6 digits."));
            }

            using var content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            using var response = await _httpClient.PostAsync(
                "authenticate/verify-mfa",
                content,
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(30);

                return Result<AuthResponse>.Failure(
                    AuthResponse.Failure($"Too many attempts. Please try again after {retryAfter.TotalSeconds} seconds."));
            }

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<Result<AuthResponse>>(
                    cancellationToken: cancellationToken);

                return result ?? Result<AuthResponse>.Failure(
                    AuthResponse.Failure("Received empty response from MFA service"));
            }

            var error = await TryReadErrorResponse(response, cancellationToken);
            _logger.LogWarning("Authentication failed for {Username}. Status: {StatusCode}, Error: {ErrorMessage}",
                    error.Value.CorrelationId,
                    response.StatusCode,
                    error.Value.Error);

            throw new AuthenticationException(
                error.Value.Error ?? "Authentication failed",
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
            return Result<AuthResponse>.Failure(
                    AuthResponse.Failure($"MFA verification failed: {ex.Message}"));
        }
    }

    public async Task<ClientDto> GetClientAsync(string clientId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<Result<ClientDto>>(
                $"client/{clientId}",
                cancellationToken);

            if (response?.Value == null)
            {
                _logger.LogWarning("Client {ClientId} not found", clientId);
                throw new NotFoundException($"Client {clientId} not found");
            }

            return response.Value;
        }
        catch (Exception ex) when (ex is not NotFoundException)
        {
            _logger.LogError(ex, "Error retrieving client {ClientId}", clientId);
            throw;
        }
    }

    private static async Task<Result<ApiError>> TryReadErrorResponse(
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