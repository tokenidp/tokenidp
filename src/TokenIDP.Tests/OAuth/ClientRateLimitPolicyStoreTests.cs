using FluentAssertions;
using Moq;
using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Abstractions.Repositories;
using TokenIDP.Core.OAuth.RateLimiting;
using TokenIDP.Domain.AggregateRoots.Clients;

namespace TokenIDP.Tests.OAuth;

public sealed class ClientRateLimitPolicyStoreTests
{
    [Fact]
    public async Task GetAsync_ShouldUseCache_AndAvoidRepeatedRepositoryCalls()
    {
        var repository = new Mock<IClientRepository>();
        repository
            .Setup(x => x.FindRateLimitProfileAsync("client-a", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClientRateLimitProfile(
                "client-a",
                7,
                10,
                5,
                TimeSpan.FromMinutes(1)));

        var sut = new ClientRateLimitPolicyStore(repository.Object, new TestCache());

        var first = await sut.GetAsync("client-a", CancellationToken.None);
        var second = await sut.GetAsync("client-a", CancellationToken.None);

        first.Should().NotBeNull();
        second.Should().BeEquivalentTo(first);
        repository.Verify(
            x => x.FindRateLimitProfileAsync("client-a", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private sealed class TestCache : ICache
    {
        private readonly Dictionary<string, object?> _entries = new(StringComparer.Ordinal);

        public Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
        {
            if (_entries.TryGetValue(key, out var cached))
            {
                return Task.FromResult((T)cached!);
            }

            return CreateAsync(key, factory);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            _entries[key] = value;
            return Task.CompletedTask;
        }

        public Task<T?> GetAsync<T>(string key)
        {
            return Task.FromResult(
                _entries.TryGetValue(key, out var cached)
                    ? (T?)cached
                    : default);
        }

        public Task RemoveAsync(string key)
        {
            _entries.Remove(key);
            return Task.CompletedTask;
        }

        public Task RemoveByPrefixAsync(string prefix)
        {
            var keys = _entries.Keys
                .Where(key => key.StartsWith(prefix, StringComparison.Ordinal))
                .ToList();

            foreach (var key in keys)
            {
                _entries.Remove(key);
            }

            return Task.CompletedTask;
        }

        private async Task<T> CreateAsync<T>(string key, Func<Task<T>> factory)
        {
            var created = await factory();
            _entries[key] = created;
            return created;
        }
    }
}
