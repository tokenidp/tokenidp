using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Abstractions.Repositories;
using TokenIDP.Domain.AggregateRoots.Authorization;

namespace TokenIDP.Infrastructure.Persistence;

internal sealed class AuthorizationRepository : IAuthorizationRepository
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICache _cache;
    private readonly IAppLogger<AuthorizationRepository> _logger;

    public AuthorizationRepository(ApplicationDbContext applicationDbContext,
        ICache cache,
        IAppLogger<AuthorizationRepository> logger)
    {
        _dbContext = applicationDbContext;
        _cache = cache;
        _logger = logger;
    }

    public async Task<AuthorizationCode?> GetByAuthCode(string code, string clientId)
    {
        var cacheKey = CacheKeys.AUTHORIZATION.FormatCacheKey(code);

        var authorizationCode = await _cache.GetAsync<AuthorizationCode>(cacheKey);

        if (authorizationCode == null)
        {
            authorizationCode = await _dbContext.AuthorizationCodes
                .FirstOrDefaultAsync(x => x.ClientId == clientId && x.Code == code
                && x.Expiry > DateTime.UtcNow && !x.IsUsed);
        }

        return authorizationCode;
    }

    public async Task<int> CreateAuthorization(AuthorizationCode authorizationCode)
    {
        _dbContext.AuthorizationCodes.Add(authorizationCode);

        await _dbContext.SaveChangesAsync();

        var cacheKey = CacheKeys.AUTHORIZATION
            .FormatCacheKey(authorizationCode.Code);

        await _cache.SetAsync(cacheKey, authorizationCode, new TimeSpan(0, 5, 0));

        return authorizationCode.Id;
    }

    public async Task<int> UpdateAuthorization(AuthorizationCode authorizationCode)
    {
        var cacheKey = CacheKeys.AUTHORIZATION.FormatCacheKey(authorizationCode.Code);

        authorizationCode.UpdateIsUsed(true);

        var id = await _dbContext.SaveChangesAsync();

        await _cache.RemoveAsync(cacheKey);

        return authorizationCode.Id;
    }

    public async Task<PreAuthorization> GetPreAuthorization(string correlationId)
    {
        var cacheKey = CacheKeys.PRE_AUTHORIZATION.FormatCacheKey(correlationId);

        var preAuthorization = await _cache.GetAsync<PreAuthorization>(cacheKey);

        if (preAuthorization == null)
        {
            preAuthorization = await _dbContext.PreAuthorizations
                   .Where(t => t.CorrelationId == correlationId
                   && t.Expiry > DateTime.UtcNow)
                   .OrderByDescending(t => t.Id)
                   .FirstOrDefaultAsync();
        }

        return preAuthorization!;
    }

    public async Task<int> CreatePreAuthorization(PreAuthorization preAuthorization, CancellationToken ct)
    {
        _dbContext.PreAuthorizations.Add(preAuthorization);

        var id = await _dbContext.SaveChangesAsync(ct);

        var cacheKey = CacheKeys.PRE_AUTHORIZATION
            .FormatCacheKey(preAuthorization.CorrelationId);

        await _cache.SetAsync(cacheKey, preAuthorization, new TimeSpan(0, 10, 0));

        return id;
    }

    public async Task<int> UpdatePreAuthorization(PreAuthorization preAuthorization)
    {
        _dbContext.PreAuthorizations.Update(preAuthorization);

        var id = await _dbContext.SaveChangesAsync();

        var cacheKey = CacheKeys.PRE_AUTHORIZATION
            .FormatCacheKey(preAuthorization.CorrelationId);

        await _cache.RemoveAsync(cacheKey);

        return preAuthorization.Id;
    }

    public async Task<int> CreateDeviceAuthorization(DeviceAuthorization deviceAuthorization)
    {
        _dbContext.DeviceAuthorizations.Add(deviceAuthorization);

        var id = await _dbContext.SaveChangesAsync();

        var cacheKey = CacheKeys.DEVICE_AUTHORIZATION
            .FormatCacheKey(deviceAuthorization.UserCodeHash);

        await _cache.SetAsync(cacheKey, deviceAuthorization, new TimeSpan(0, 5, 0));

        return id;
    }

    public async Task<int> UpdateDeviceAuthorization(DeviceAuthorization deviceAuthorization)
    {
        _dbContext.DeviceAuthorizations.Update(deviceAuthorization);

        var id = await _dbContext.SaveChangesAsync();

        var cacheKey = CacheKeys.DEVICE_AUTHORIZATION
            .FormatCacheKey(deviceAuthorization.DeviceCodeHash);

        await _cache.SetAsync(cacheKey, deviceAuthorization, new TimeSpan(0, 5, 0));

        return deviceAuthorization.Id;
    }

    public async Task<DeviceAuthorization?> GetDeviceAuthorizationByUCode(string userCodeHash)
    {
        var cacheKey = CacheKeys.DEVICE_AUTHORIZATION.FormatCacheKey(userCodeHash);

        var deviceAuthorization = await _cache.GetAsync<DeviceAuthorization>(cacheKey);

        if (deviceAuthorization == null)
        {
            deviceAuthorization = await _dbContext.DeviceAuthorizations
                   .Where(t => t.UserCodeHash == userCodeHash
                                && t.ExpiresAtUtc > DateTime.UtcNow)
                   .OrderByDescending(t => t.Id)
                   .FirstOrDefaultAsync();
        }

        return deviceAuthorization;
    }

    public async Task<DeviceAuthorization?> GetDeviceAuthorizationByDCode(string deviceCodeHash)
    {
        var cacheKey = CacheKeys.DEVICE_AUTHORIZATION.FormatCacheKey(deviceCodeHash);

        var deviceAuthorization = await _cache.GetAsync<DeviceAuthorization>(cacheKey);

        if (deviceAuthorization == null)
        {
            deviceAuthorization = await _dbContext.DeviceAuthorizations
                   .Where(t => t.DeviceCodeHash == deviceCodeHash
                                && t.ExpiresAtUtc > DateTime.UtcNow)
                   .OrderByDescending(t => t.Id)
                   .FirstOrDefaultAsync();
        }

        return deviceAuthorization;
    }

    public async Task<int> CreateBackchannelAuthenticationRequest(
        BackchannelAuthenticationRequest request,
        CancellationToken ct)
    {
        _dbContext.BackchannelAuthenticationRequests.Add(request);

        await _dbContext.SaveChangesAsync(ct);

        await _cache.SetAsync(
            CacheKeys.CIBA_AUTHORIZATION.FormatCacheKey(request.AuthReqIdHash),
            request,
            TimeSpan.FromMinutes(5));

        return request.Id;
    }

    public async Task<int> UpdateBackchannelAuthenticationRequest(
        BackchannelAuthenticationRequest request,
        CancellationToken ct)
    {
        _dbContext.BackchannelAuthenticationRequests.Update(request);

        await _dbContext.SaveChangesAsync(ct);

        await _cache.SetAsync(
            CacheKeys.CIBA_AUTHORIZATION.FormatCacheKey(request.AuthReqIdHash),
            request,
            TimeSpan.FromMinutes(5));

        return request.Id;
    }

    public async Task<BackchannelAuthenticationRequest?> GetBackchannelAuthenticationRequestByHashAsync(
        string authReqIdHash,
        CancellationToken ct)
    {
        var cacheKey = CacheKeys.CIBA_AUTHORIZATION.FormatCacheKey(authReqIdHash);

        var request = await _cache.GetAsync<BackchannelAuthenticationRequest>(cacheKey);

        if (request != null)
        {
            return request;
        }

        request = await _dbContext.BackchannelAuthenticationRequests
            .FirstOrDefaultAsync(x => x.AuthReqIdHash == authReqIdHash, ct);

        if (request != null)
        {
            await _cache.SetAsync(cacheKey, request, TimeSpan.FromMinutes(5));
        }

        return request;
    }

    public Task<BackchannelAuthenticationRequest?> GetBackchannelAuthenticationRequestByIdAsync(
        int id,
        CancellationToken ct)
    {
        return _dbContext.BackchannelAuthenticationRequests
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<IReadOnlyList<BackchannelAuthenticationRequest>> GetPendingBackchannelRequestsForUserAsync(
        int tenantId,
        int userId,
        CancellationToken ct)
    {
        return await _dbContext.BackchannelAuthenticationRequests
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.UserId == userId &&
                x.Status == CibaRequestStatus.AwaitingAuthorization &&
                x.ExpiresAtUtc > DateTime.UtcNow)
            .OrderByDescending(x => x.Id)
            .ToListAsync(ct);
    }
}


