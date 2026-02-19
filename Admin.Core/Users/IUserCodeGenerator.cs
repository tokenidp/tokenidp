namespace Admin.Core.Users;

public interface IUserCodeGenerator
{
    Task<int> GenerateNextValueAsync(int tenantId, CancellationToken ct);
}
