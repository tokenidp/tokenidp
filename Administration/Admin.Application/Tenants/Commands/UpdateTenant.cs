namespace Identity.Application.Tenants.Commands;

public class UpdateTenant : IRequest<Result>
{
    public int Id { get; set; }
    public string TenantName { get; set; }
    public string TenantCode { get; set; }
    public string Email { get; set; }
    public string Theme { get; set; }
    public string Logo { get; set; }
    public string TenantAppId { get; set; }
    public string LandingPage { get; set; }
    public bool? IsActive { get; set; }
}

public class UpdateTenantCommandHandler : IRequestHandler<UpdateTenant, Result>
{
    private readonly IApplicationDbContext _dbContext;

    public UpdateTenantCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result> Handle(UpdateTenant request, CancellationToken cancellationToken)
    {
        var tenant = await _dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (tenant == null)
        {
            return Result.Failure("NotFound", "Tenant not found for the Id {0}".FormatString(request.Id));
        }

        tenant.UpdateTenant(request.TenantName,
              request.Email,
              request.Theme,
              request.Logo,
              request.TenantAppId,
              request.LandingPage,
              request.IsActive);

        _dbContext.Tenants.Update(tenant);

        var result = await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(result);
    }
}