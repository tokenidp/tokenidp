using IDP.Core.Model;
using IDP.Foundation;
using IDP.Foundation.Abstractions;
using IDP.Foundation.Contracts;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace IDP.Server.RESTClients;

public class AuthenticationClient : IDisposable
{

    protected readonly HttpClient _httpClient;
    protected readonly IAppLogger<AuthenticationClient> _logger;

    private bool _disposed;

    public AuthenticationClient(
        IHttpClientFactory httpClientFactory,
        IAppLogger<AuthenticationClient> logger)
    {
        _httpClient = httpClientFactory.CreateClient("IDPClient");
        _logger = logger;
    }

    public async Task<AuthResponseDto> AuthenticateAsync(AuthorizationRequest request,
        string? antiforgeryToken, // Add this parameter
        CancellationToken cancellationToken = default)
    {
        try
        {
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "local-login")
            {
                Content = JsonContent.Create(request)
            };

            if (!string.IsNullOrEmpty(antiforgeryToken))
            {
                // 'RequestToken' is the standard header name ASP.NET Core looks for
                httpRequest.Headers.Add("X-XSRF-TOKEN", antiforgeryToken);
            }

            if (!string.IsNullOrEmpty(antiforgeryToken))
            {
                // 'RequestToken' is the standard header name ASP.NET Core looks for
                httpRequest.Headers.Add("X-XSRF-TOKEN", antiforgeryToken);
            }

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = await TryReadErrorResponse(response, cancellationToken);

                _logger.LogWarning("Authentication failed for {Username}. Status: {StatusCode}, Error: {ErrorMessage}",
                    request.UserName,
                    response.StatusCode,
                    errorResponse?.Value?.Error ?? "invalid request");

                throw new AuthenticationException(
                    errorResponse?.Value?.Error ?? "Authentication failed",
                    (int)response.StatusCode);
            }

            var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>(
                cancellationToken: cancellationToken);

            _logger.LogInfo("Authentication successful for {Username}", request.UserName);

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
            _logger.LogInfo("AuthenticationService disposed");
        }

        _disposed = true;
    }
}