using IDP.Web.Model;
using System.Net;
using System.Text;
using System.Text.Json;

namespace IDP.Web.Authentication
{
    public class MfaService : BaseService<MfaService>
    {
        public MfaService(
            IHttpClientFactory httpClientFactory,
            ILogger<MfaService> logger) : base(httpClientFactory, logger)
        {

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
                    "auth/resend-mfa",
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
                    "auth/verify-mfa",
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
    }
}