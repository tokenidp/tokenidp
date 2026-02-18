using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace IDP.Infrastructure.Persistence;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(
                "Server=.;Database=IDP_Dev;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        var systemUser = new SystemCurrentUserService();

        return new ApplicationDbContext(options, systemUser);
    }
}

