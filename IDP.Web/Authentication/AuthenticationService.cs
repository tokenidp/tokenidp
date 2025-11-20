using IDP.Web.Model;

namespace IDP.Web.Authentication;

public class AuthenticationService : BaseService<AuthenticationService>
{
    public AuthenticationService(
        IHttpClientFactory httpClientFactory,
        ILogger<AuthenticationService> logger) : base(httpClientFactory, logger)
    {

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
}