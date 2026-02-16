namespace IDP.Infrastructure.Config;

internal class UserConfig : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.FirstName).HasMaxLength(50).IsRequired();
        builder.Property(x => x.LastName).HasMaxLength(50).IsRequired();
        builder.Property(x => x.UserName).HasMaxLength(50).IsRequired();
        builder.Property(x => x.NormalizedUserName).HasMaxLength(50).IsRequired();

        builder.Property(x => x.Email).HasMaxLength(100).IsRequired();
        builder.Property(x => x.NormalizedEmail).HasMaxLength(100).IsRequired();

        builder.Property(x => x.PasswordHash).HasMaxLength(256).IsRequired();
        builder.Property(x => x.PhoneNumber).HasMaxLength(20);
        builder.Property(u => u.ConcurrencyStamp).HasMaxLength(100).IsConcurrencyToken();

        builder.Property(x => x.StatusId).HasMaxLength(20).IsRequired();
        builder.Property(p => p.StatusId)
            .HasConversion(
                v => v.ToString(),
                v => (UserStatus)Enum.Parse(typeof(UserStatus), v));

        builder.Property(x => x.CreatedAtUtc).IsRequired();

        // Computed column for audit
        builder.Property(x => x.EffectiveUserId)
            .HasComputedColumnSql(
                "COALESCE(NULLIF([UpdatedBy], 0), [CreatedBy])",
                stored: true);

        builder.HasMany(e => e.UserRoles)
        .WithOne(e => e.User)
        .HasForeignKey(ur => ur.UserId)
        .IsRequired();

        builder.HasMany(e => e.UserAddresses)
        .WithOne(e => e.User)
        .HasForeignKey(ur => ur.UserId)
        .IsRequired();

        builder.HasMany(e => e.UserContacts)
          .WithOne(e => e.User)
          .HasForeignKey(ur => ur.UserId)
          .IsRequired();

        // Uniqueness per tenant
        builder.HasIndex(x => new { x.TenantId, x.UserName })
            .IsUnique()
            .HasDatabaseName("IX_Users_Tenant_UserName");

        builder.HasIndex(x => new { x.TenantId, x.Email })
            .IsUnique()
            .HasDatabaseName("IX_Users_Tenant_Email");

        builder.HasIndex(x => new { x.TenantId, x.UserCode })
            .IsUnique()
            .HasDatabaseName("IX_Users_Tenant_UserCode");

        // Login hot path (username/email)
        builder.HasIndex(x => new { x.TenantId, x.UserName, x.StatusId })
            .HasDatabaseName("IX_Users_Login_ByUserName");

        builder.HasIndex(x => new { x.TenantId, x.Email, x.StatusId })
            .HasDatabaseName("IX_Users_Login_ByEmail");

        // Admin listing
        builder.HasIndex(x => new { x.TenantId, x.CreatedAtUtc })
            .HasDatabaseName("IX_Users_Tenant_Time");
    }
}
