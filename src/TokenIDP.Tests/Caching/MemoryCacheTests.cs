using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using TokenIDP.Core.Abstractions;
using InfrastructureMemoryCache = TokenIDP.Infrastructure.MemoryCache;

namespace TokenIDP.Tests.Caching;

public sealed class MemoryCacheTests
{
    [Fact]
    public async Task RemoveByPrefixAsync_ShouldEvictMatchingKeysOnly()
    {
        var cache = new InfrastructureMemoryCache(
            new Microsoft.Extensions.Caching.Memory.MemoryCache(new MemoryCacheOptions()),
            Mock.Of<IAppLogger<InfrastructureMemoryCache>>());

        await cache.SetAsync("urn:USC:7:tenants.edit", "allowed");
        await cache.SetAsync("urn:USR:7:Administrator", "assigned");
        await cache.SetAsync("urn:USC:8:tenants.edit", "allowed");

        await cache.RemoveByPrefixAsync("urn:USC:7");

        var removedValue = await cache.GetAsync<string>("urn:USC:7:tenants.edit");
        var retainedRoleValue = await cache.GetAsync<string>("urn:USR:7:Administrator");
        var retainedOtherUserValue = await cache.GetAsync<string>("urn:USC:8:tenants.edit");

        removedValue.Should().BeNull();
        retainedRoleValue.Should().Be("assigned");
        retainedOtherUserValue.Should().Be("allowed");
    }
}
