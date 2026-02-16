using Admin.Core.Tenants;

namespace IDP.Infrastructure.Bootstrap.SeedData;

internal class DefaultTenants
{
    public static readonly CreateUpdateTenant SystemTenant = new(
         tenantName: "system",
         tenantCode: "System001",
         email: "admin@system.local",
         theme: "default",
         logoUrl: "default",
         primaryColor: "#0F172A",
         defaultLanguage: "en",
         loginText: "Identity Platform Administration",
         twoFactorEnabled: true,
         twoFactorCodeExpiry: 300,
         homePageUrl: "/dashboard",
         isActive: true,
         tenantType: TenantTypes.Internal,
         subscriptionType: SubscriptionTypes.Paid,
         authenticationMode: AuthenticationModes.Local
     );
}