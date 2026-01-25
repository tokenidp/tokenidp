namespace IDP.Infrastructure.Config;

internal class UserContactConfig : IEntityTypeConfiguration<UserContact>
{
    public void Configure(EntityTypeBuilder<UserContact> builder)
    {
        builder.HasKey(p => new { p.Id });

        builder.ToTable("UserContacts");
    }
}
