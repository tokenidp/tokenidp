namespace Identity.Infrastructure.Configurations;

public class UserClaimConfig : IEntityTypeConfiguration<UserClaim>
{
    public void Configure(EntityTypeBuilder<UserClaim> builder)
    {
        builder.HasKey(p => new { p.Id });

        builder.ToView("vUserClaims");
    }
}
