using static Identity.Domain.Entities.AppUser;

namespace Identity.Infrastructure.Configurations;

public class AppUserConfig : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.HasKey(p => new { p.Id });

        builder.ToTable("AppUsers");

        builder.Property(u => u.ConcurrencyStamp)
        .IsConcurrencyToken();

        builder.HasMany(e => e.AppUserRoles)
        .WithOne(e => e.AppUser)
        .HasForeignKey(ur => ur.UserId)
        .IsRequired();

        builder.Property(p => p.StatusId)
        .HasConversion(
        v => v.ToString(),
        v => (UserStatus)Enum.Parse(typeof(UserStatus), v));
    }
}
