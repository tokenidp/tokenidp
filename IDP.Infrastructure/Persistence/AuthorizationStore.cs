using IDP.Domain.AggregateRoots.Authorization;
using IDP.Domain.AggregateRoots.Users;
using IDP.Foundation.Abstractions.Stores;

namespace IDP.Infrastructure.Persistence;

internal sealed class AuthorizationStore : IAuthorizationStore
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICache _cache;
    private readonly IAppLogger<AuthorizationStore> _logger;

    public AuthorizationStore(IApplicationDbContext applicationDbContext,
        ICache cache,
        IAppLogger<AuthorizationStore> logger)
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

    public async Task<PreAuthorization> GetPreAuthorization(string correlationId, int userId)
    {
        var cacheKey = CacheKeys.PRE_AUTHORIZATION.FormatCacheKey(correlationId, userId);

        var preAuthorization = await _cache.GetAsync<PreAuthorization>(cacheKey);

        if (preAuthorization == null)
        {
            preAuthorization = await _dbContext.PreAuthorizations
                   .Where(t => t.CorrelationId == correlationId && t.UserId == userId
                                && t.Expiry > DateTime.UtcNow && !t.Is2FAVerified)
                   .OrderByDescending(t => t.Id)
                   .FirstOrDefaultAsync();
        }

        return preAuthorization!;
    }

    public async Task<int> CreatePreAuthorization(PreAuthorization preAuthorization)
    {
        _dbContext.PreAuthorizations.Add(preAuthorization);

        var id = await _dbContext.SaveChangesAsync();

        var cacheKey = CacheKeys.PRE_AUTHORIZATION
            .FormatCacheKey(preAuthorization.CorrelationId, preAuthorization.UserId);

        await _cache.SetAsync(cacheKey, preAuthorization, new TimeSpan(0, 5, 0));

        return id;
    }

    public async Task<int> UpdatePreAuthorization(PreAuthorization preAuthorization)
    {
        _dbContext.PreAuthorizations.Update(preAuthorization);

        var id = await _dbContext.SaveChangesAsync();

        var cacheKey = CacheKeys.PRE_AUTHORIZATION
            .FormatCacheKey(preAuthorization.CorrelationId, preAuthorization.UserId);

        await _cache.SetAsync(cacheKey, preAuthorization, new TimeSpan(0, 5, 0));

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
}
