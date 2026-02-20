using Admin.Core.Users;

namespace IDP.Infrastructure.Bootstrap.SeedData;

internal static class DefaultUsers
{
    public static UserDetail Admin(string adminName, string tempPassword) => new()
    {
        UserName = adminName,
        NormalizedUserName = adminName.ToUpper(),
        Email = "admin@system.local",
        FirstName = "System",
        LastName = "Administrator",
        Phone = "123456789",

        Status = "Active",
        EmailConfirmed = true,
        PhoneNumberConfirmed = false,

        TwoFactorEnabled = false,
        LockoutEnabled = true,
        AccessFailedCount = 0,

        Password = tempPassword
    };
}