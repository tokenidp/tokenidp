using TokenIDP.Core.Abstractions.Repositories;
using TokenIDP.Core.Foundation.Security;
using TokenIDP.Domain;
using TokenIDP.Domain.DomainEvents.Activities;
using TokenIDP.Domain.ReadModels.Enums;

namespace TokenIDP.Core.OAuth.UseCases;

internal sealed class CibaApprovalUseCase
{
    private readonly IAuthorizationRepository _authorizationRepository;
    private readonly IClientRepository _clientRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationEventDispatcher _applicationEventDispatcher;

    public CibaApprovalUseCase(
        IAuthorizationRepository authorizationRepository,
        IClientRepository clientRepository,
        ICurrentUserService currentUserService,
        IApplicationEventDispatcher applicationEventDispatcher)
    {
        _authorizationRepository = authorizationRepository;
        _clientRepository = clientRepository;
        _currentUserService = currentUserService;
        _applicationEventDispatcher = applicationEventDispatcher;
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
        request.Approve(
            decisionByUserId: _currentUserService.UserId,
            decisionIpAddress: _currentUserService.IpAddress,
            decisionUserAgent: _currentUserService.UserAgent);
        await _authorizationRepository.UpdateBackchannelAuthenticationRequest(request, ct);
        return ApiResult<int>.Success(request.Id);
    }

    public async Task<ApiResult<int>> DenyAsync(int requestId, string? reason, CancellationToken ct)
    {
        var request = await LoadOwnedRequestAsync(requestId, ct);
        request.Deny(
            reason,
            _currentUserService.UserId,
            _currentUserService.IpAddress,
            _currentUserService.UserAgent);
        await _authorizationRepository.UpdateBackchannelAuthenticationRequest(request, ct);
        return ApiResult<int>.Success(request.Id);
    }

    public async Task<CibaApprovalChallenge> GetApprovalChallengeAsync(
        Guid publicRequestId,
        string approvalToken,
        bool recordPageOpened,
        CancellationToken ct)
    {
        var request = await LoadByPublicIdAndTokenAsync(publicRequestId, approvalToken, ct);

        if (request.IsExpired())
        {
            request.EnsureNotExpired();
        }

        if (recordPageOpened)
        {
            await _applicationEventDispatcher.RaiseAsync(
                CreateActivity(
                    request,
                    ActivityEventType.CibaApprovalPageOpened,
                    "Opened",
                    "CIBA approval page opened."),
                ct);
        }

        var client = await _clientRepository.GetClientShortInfo(request.ClientId);
        return new CibaApprovalChallenge
        {
            PublicRequestId = request.PublicRequestId,
            TenantId = request.TenantId,
            UserId = request.UserId.GetValueOrDefault(),
            ClientId = request.ClientId,
            ClientName = client.ClientName,
            BindingMessage = request.BindingMessage ?? string.Empty,
            RequestedScopes = request.RequestedScopes,
            ExpiresAtUtc = request.ExpiresAtUtc
        };
    }

    public async Task ApproveWithTokenAsync(
        Guid publicRequestId,
        string approvalToken,
        string? ipAddress,
        string? userAgent,
        CancellationToken ct)
    {
        var request = await LoadByPublicIdAndTokenAsync(publicRequestId, approvalToken, ct);

        request.ConsumeApprovalToken();
        request.Approve(
            decisionByUserId: request.UserId,
            decisionIpAddress: ipAddress,
            decisionUserAgent: userAgent);

        await _authorizationRepository.UpdateBackchannelAuthenticationRequest(request, ct);
        await _applicationEventDispatcher.RaiseAsync(
            CreateActivity(request, ActivityEventType.CibaRequestApproved, "Approved", "CIBA request approved."),
            ct);
    }

    public async Task RejectWithTokenAsync(
        Guid publicRequestId,
        string approvalToken,
        string? ipAddress,
        string? userAgent,
        CancellationToken ct)
    {
        var request = await LoadByPublicIdAndTokenAsync(publicRequestId, approvalToken, ct);

        request.ConsumeApprovalToken();
        request.Deny(
            "Rejected by user",
            request.UserId,
            ipAddress,
            userAgent);

        await _authorizationRepository.UpdateBackchannelAuthenticationRequest(request, ct);
        await _applicationEventDispatcher.RaiseAsync(
            CreateActivity(request, ActivityEventType.CibaRequestRejected, "Rejected", "CIBA request rejected."),
            ct);
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

    private async Task<BackchannelAuthenticationRequest> LoadByPublicIdAndTokenAsync(
        Guid publicRequestId,
        string approvalToken,
        CancellationToken ct)
    {
        if (publicRequestId == Guid.Empty || string.IsNullOrWhiteSpace(approvalToken))
        {
            throw new NotFoundException("CIBA request not found.");
        }

        var request = await _authorizationRepository
            .GetBackchannelAuthenticationRequestByPublicIdAsync(publicRequestId, ct);

        if (request == null)
        {
            throw new NotFoundException("CIBA request not found.");
        }

        try
        {
            request.EnsureNotExpired();
            request.EnsureApprovalTokenCanBeUsed(SecretHasher.HashSecret(approvalToken));
        }
        catch (DomainException ex) when (ex.Message == "expired_token")
        {
            await _authorizationRepository.UpdateBackchannelAuthenticationRequest(request, ct);
            await _applicationEventDispatcher.RaiseAsync(
                CreateActivity(request, ActivityEventType.CibaRequestExpired, "Expired", "CIBA request expired."),
                ct);
            throw;
        }

        return request;
    }

    private static ActivityDomainEvent CreateActivity(
        BackchannelAuthenticationRequest request,
        ActivityEventType eventType,
        string status,
        string description)
        => new(
            TenantId: request.TenantId,
            EventType: eventType,
            AggregateType: "CibaRequest",
            AggregateId: request.PublicRequestId.ToString("D"),
            ActorId: request.UserId?.ToString(),
            ActorDisplayName: null,
            TargetId: request.PublicRequestId.ToString("D"),
            TargetDescription: request.ClientId,
            Status: status,
            Description: description);
}
