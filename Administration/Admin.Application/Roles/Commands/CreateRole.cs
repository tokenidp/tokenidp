namespace Identity.Application.Roles.Commands;

public class CreateRole : IRequest<Result>
{
    public string Name { get; set; }
    public string RoleDescription { get; set; }
    public int TenantId { get; set; }
    public bool? IsActive { get; set; }
}

public class CreateRoleCommandHandler : IRequestHandler<CreateRole, Result>
{
    private readonly IApplicationDbContext _dbContext;

    public CreateRoleCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result> Handle(CreateRole request, CancellationToken cancellationToken)
    {
        AppRole appRole = new(
            request.TenantId,
            request.Name,
            request.RoleDescription,
            request.IsActive
            );

        _dbContext.AppRoles.Add(appRole);

        var result = await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(result);
    }
}
