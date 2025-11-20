namespace IDP.Core.Infrastructure.Config;

public class UserSearchConfig : IEntityTypeConfiguration<UserSearch>
{
    public void Configure(EntityTypeBuilder<UserSearch> builder)
    {
        builder.HasKey(p => new { p.Id });

        builder.ToView("vUserSearch");
    }
}
