namespace Identity.Application.Roles.Commands;

public class UpdateRole : IRequest<Result>
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string RoleDescription { get; set; }
    public bool? IsActive { get; set; }
}

public class UpdateRoleCommandHandler : IRequestHandler<UpdateRole, Result>
{
    private readonly IApplicationDbContext _dbContext;

    public UpdateRoleCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result> Handle(UpdateRole request, CancellationToken cancellationToken)
    {
        var role = await _dbContext.AppRoles.FirstOrDefaultAsync(r => r.Id == request.Id, CancellationToken.None);

        if (role == null)
        {
            return Result.Failure("NotFound", "Role not found for the Id {0}".FormatString(request.Id));
        }

        role.UpdateRole(
            request.Name,
            request.RoleDescription,
            request.IsActive
            );

        _dbContext.AppRoles.Update(role);

        var result = await _dbContext.SaveChangesAsync();

        return Result.Success(result);
    }
}
