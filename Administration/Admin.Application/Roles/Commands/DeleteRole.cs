namespace Identity.Application.Roles.Commands;

public class DeleteRole : IRequest<Result>
{
    public int Id { get; set; }
}

public class DeleteRoleCommandHandler : IRequestHandler<DeleteRole, Result>
{
    private readonly IApplicationDbContext _dbContext;

    public DeleteRoleCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result> Handle(DeleteRole request, CancellationToken cancellationToken)
    {
        var role = await _dbContext.AppRoles.FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (role == null)
        {
            return Result.Failure("NotFound", "Role not found for the Id {0}".FormatString(request.Id));
        }

        role.DeleteRole();

        _dbContext.AppRoles.Update(role);

        var result = await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(result);
    }
}
