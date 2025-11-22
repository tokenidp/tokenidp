using static IDP.Core.Domain.User;

namespace IDP.Core.Infrastructure.Config;

internal class UserConfig : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(p => new { p.Id });

        builder.ToTable("Users");

        builder.Property(u => u.ConcurrencyStamp)
        .IsConcurrencyToken();

        builder.HasMany(e => e.UserRoles)
        .WithOne(e => e.User)
        .HasForeignKey(ur => ur.UserId)
        .IsRequired();

        builder.Property(p => p.StatusId)
        .HasConversion(
        v => v.ToString(),
        v => (UserStatus)Enum.Parse(typeof(UserStatus), v));
    }
}
