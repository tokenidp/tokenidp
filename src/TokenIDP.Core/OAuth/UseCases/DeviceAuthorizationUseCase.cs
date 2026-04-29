using TokenIDP.Core.Abstractions.Repositories;
using TokenIDP.Core.Foundation.Security;

namespace TokenIDP.Core.OAuth.UseCases;

internal sealed class DeviceAuthorizationUseCase
{
    private readonly IAuthorizationRepository _authorizationStore;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuthorizationRequestValidator _authorizationRequestValidator;
    private readonly IAppLogger<DeviceAuthorizationUseCase> _logger;

    public DeviceAuthorizationUseCase(
        ICurrentUserService currentUserService,
        IAuthorizationRepository authorizationStore,
        IAppLogger<DeviceAuthorizationUseCase> logger,
        IAuthorizationRequestValidator authorizationRequestValidator)
    {
        _currentUserService = currentUserService;
        _authorizationStore = authorizationStore;
        _logger = logger;
        _authorizationRequestValidator = authorizationRequestValidator;
    }

    internal async Task<DeviceAuthorizationResponse> CreateAsync(
        DeviceAuthorizationRequest request,
        CancellationToken ct)
    {
        try
        {
            var clientInfo = await _authorizationRequestValidator.ValidateAsync(request, CancellationToken.None);

            _logger.LogInfo(
                "Device authorization request started. " +
                "TenantId: {TenantId}, ClientId: {ClientId}, " +
                "Scopes: {Scopes}, PKCE: {HasPkce}, " +
                "DeviceMetadata: {DeviceMetadata}",
                clientInfo.TenantId,
                request.ClientId,
                request.Scope,
                !string.IsNullOrWhiteSpace(request.CodeChallenge),
                request.DeviceMetadata ?? string.Empty);

            var deviceCode = DeviceCodeGenerator.GenerateDeviceCode();
            var userCode = DeviceCodeGenerator.GenerateUserCode();

            var deviceCodeHash = SecretHasher.HashSecret(deviceCode);
            var userCodeHash = SecretHasher.HashSecret(userCode);

            var expiresIn = 600;
            var interval = 5;

            var deviceAuthorization = DeviceAuthorization.Create(
                clientInfo.TenantId,
                request.ClientId,
                deviceCodeHash,
                userCodeHash,
                request.Scope,
                expiresIn,
                interval,
                request.CodeChallenge,
                request.CodeChallengeMethod,
                request.DeviceMetadata);

            await _authorizationStore.CreateDeviceAuthorization(deviceAuthorization);

            _logger.LogInfo(
                "Device authorization created successfully. " +
                "RequestId: {RequestId}, TenantId: {TenantId}, " +
                "ClientId: {ClientId}, ExpiresIn: {ExpiresIn}, " +
                "Interval: {Interval}, DeviceCodeHash: {DeviceCodeHash}",
                deviceAuthorization.Id,
                clientInfo.TenantId,
                request.ClientId,
                expiresIn,
                interval,
                deviceCodeHash);

            return new DeviceAuthorizationResponse
            {
                DeviceCode = deviceCode,
                UserCode = userCode,
                VerificationUri = $"{_currentUserService.BaseUrl}/device",
                VerificationUriComplete =
                    $"{_currentUserService.BaseUrl}/device?user_code={Uri.EscapeDataString(userCode)}",
                ExpiresIn = expiresIn,
                Interval = interval
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Device authorization creation failed. " +
                "ClientId: {ClientId}",
                request.ClientId);

            throw;
        }
    }
}

