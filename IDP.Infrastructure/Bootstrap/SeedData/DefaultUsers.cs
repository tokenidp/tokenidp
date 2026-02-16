using Admin.Core.Users;

namespace IDP.Infrastructure.Bootstrap.SeedData;

internal static class DefaultUsers
{
    public static UserDetail Admin(string tempPassword) => new()
    {
        UserName = "admin",
        NormalizedUserName = "ADMIN",
        Email = "admin@system.local",
        FirstName = "System",
        LastName = "Administrator",
        Phone = null,

        Status = "Active",
        EmailConfirmed = true,
        PhoneNumberConfirmed = false,

        TwoFactorEnabled = false,
        LockoutEnabled = true,
        AccessFailedCount = 0,

        Password = tempPassword
    };
}