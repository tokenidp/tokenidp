

namespace Admin.Core.Clients;

internal class ClientService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICache _cache;
    private readonly IAppLogger<ClientService> _logger;

    public ClientService(ApplicationDbContext dbContext,
        IAppLogger<ClientService> logger,
        ICache cache)
    {
        _dbContext = dbContext;
        _logger = logger;
        _cache = cache;
    }
}