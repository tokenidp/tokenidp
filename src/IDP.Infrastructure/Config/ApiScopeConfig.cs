namespace IDP.Infrastructure.Config;

internal sealed class ApiScopeConfig : IEntityTypeConfiguration<ApiScope>
{
    public void Configure(EntityTypeBuilder<ApiScope> builder)
    {
        builder.ToTable("ApiScopes");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.ApiResourceId).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.Enabled).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.CreatedBy).IsRequired();

        builder.HasOne(x => x.ApiResource)
            .WithMany(x => x.Scopes)
            .HasForeignKey(x => x.ApiResourceId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.ApiResourceId, x.Name })
            .IsUnique()
            .HasDatabaseName("IX_ApiScopes_ApiResourceId_Name");

        builder.HasIndex(x => x.Name)
            .HasDatabaseName("IX_ApiScopes_Name");
    }
}
