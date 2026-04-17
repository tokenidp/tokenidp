using TokenIDP.Core.Abstractions.Repositories;

namespace TokenIDP.Core.OAuth.UseCases;

internal sealed class CibaApprovalUseCase
{
    private readonly IAuthorizationRepository _authorizationRepository;
    private readonly IClientRepository _clientRepository;
    private readonly ICurrentUserService _currentUserService;

    public CibaApprovalUseCase(
        IAuthorizationRepository authorizationRepository,
        IClientRepository clientRepository,
        ICurrentUserService currentUserService)
    {
        _authorizationRepository = authorizationRepository;
        _clientRepository = clientRepository;
        _currentUserService = currentUserService;
    }

    public async Task<ApiResult<IReadOnlyList<CibaPendingRequest>>> GetPendingAsync(CancellationToken ct)
    {
        if (_currentUserService.UserId <= 0 || _currentUserService.TenantId <= 0)
        {
            return ApiResult<IReadOnlyList<CibaPendingRequest>>.Failure(
                ApiError.Failure("Unauthorized", "An authenticated user is required."));
        }

        var requests = await _authorizationRepository.GetPendingBackchannelRequestsForUserAsync(
            _currentUserService.TenantId,
            _currentUserService.UserId,
            ct);

        var items = new List<CibaPendingRequest>(requests.Count);
        foreach (var request in requests)
        {
            var client = await _clientRepository.GetClientShortInfo(request.ClientId);
            items.Add(new CibaPendingRequest
            {
                Id = request.Id,
                ClientId = request.ClientId,
                ClientName = client.ClientName,
                RequestedScopes = request.RequestedScopes,
                BindingMessage = request.BindingMessage,
                ExpiresAtUtc = request.ExpiresAtUtc,
                CreatedAtUtc = request.CreatedAtUtc
            });
        }

        return ApiResult<IReadOnlyList<CibaPendingRequest>>.Success(items);
    }

    public async Task<ApiResult<int>> ApproveAsync(int requestId, CancellationToken ct)
    {
        var request = await LoadOwnedRequestAsync(requestId, ct);
        request.Approve();
        await _authorizationRepository.UpdateBackchannelAuthenticationRequest(request, ct);
        return ApiResult<int>.Success(request.Id);
    }

    public async Task<ApiResult<int>> DenyAsync(int requestId, string? reason, CancellationToken ct)
    {
        var request = await LoadOwnedRequestAsync(requestId, ct);
        request.Deny(reason);
        await _authorizationRepository.UpdateBackchannelAuthenticationRequest(request, ct);
        return ApiResult<int>.Success(request.Id);
    }

    private async Task<BackchannelAuthenticationRequest> LoadOwnedRequestAsync(
        int requestId,
        CancellationToken ct)
    {
        if (_currentUserService.UserId <= 0 || _currentUserService.TenantId <= 0)
        {
            throw new ForbiddenAccessException();
        }

        var request = await _authorizationRepository.GetBackchannelAuthenticationRequestByIdAsync(requestId, ct);

        if (request == null ||
            request.TenantId != _currentUserService.TenantId ||
            request.UserId != _currentUserService.UserId)
        {
            throw new NotFoundException("CIBA request not found.");
        }

        return request;
    }
}
