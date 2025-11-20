using Microsoft.Extensions.DependencyInjection;

namespace IDP.Tests.Infrastructure;

public class IntegrationTestFixture : IDisposable
{
    public ApplicationDbContext? DbContext { get; }
    private readonly CustomWebApplicationFactory<Program> _factory;
    private bool _disposed = false; // Track whether Dispose has been called

    public IntegrationTestFixture()
    {
        _factory = new CustomWebApplicationFactory<Program>();
        DbContext = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }

    public HttpClient HttpClient => _factory.CreateClient();

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                DbContext?.Dispose();
                _factory.Dispose();
            }
            _disposed = true;
        }
    }

    ~IntegrationTestFixture()
    {
        Dispose(false);
    }
}
