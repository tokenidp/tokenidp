namespace TokenIDP.Infrastructure.Persistence;

internal class ApplicationEventDispatcher : IApplicationEventDispatcher
{
    private readonly ApplicationDbContext _dbContext;

    public ApplicationEventDispatcher(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Raise(IDomainEvent evt)
    {
        // Store domain event temporarily on DbContext
        _dbContext.AddDomainEvent(evt);
    }
}
