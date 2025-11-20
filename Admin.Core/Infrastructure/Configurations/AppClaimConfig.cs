namespace Identity.Infrastructure.Configurations;

public class AppClaimConfig : IEntityTypeConfiguration<AppClaim>
{
    public void Configure(EntityTypeBuilder<AppClaim> builder)
    {
        builder.HasKey(p => new { p.Id });

        builder.ToTable("AppClaims");
    }
}
