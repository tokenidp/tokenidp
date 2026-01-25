using IDP.Domain.AggregateRoots.Authorization;

namespace IDP.Infrastructure.Config;

internal class AuthorizationCodeConfig : IEntityTypeConfiguration<AuthorizationCode>
{
    public void Configure(EntityTypeBuilder<AuthorizationCode> builder)
    {
        builder.HasKey(p => new { p.Id });

        builder.ToTable("AuthorizationCodes");
    }
}