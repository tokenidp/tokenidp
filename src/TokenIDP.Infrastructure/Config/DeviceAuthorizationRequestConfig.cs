using TokenIDP.Domain.AggregateRoots.Authorization;

namespace TokenIDP.Infrastructure.Config;

public sealed class DeviceAuthorizationRequestConfig
    : IEntityTypeConfiguration<DeviceAuthorization>
{
    public void Configure(EntityTypeBuilder<DeviceAuthorization> builder)
    {
        builder.ToTable("DeviceAuthorizationRequests");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.ClientId).HasMaxLength(200).IsRequired();

        builder.Property(x => x.DeviceCodeHash).HasMaxLength(256).IsRequired();
        builder.Property(x => x.UserCodeHash).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Scopes).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.Status).HasConversion<byte>().IsRequired();

        builder.Property(x => x.IntervalSeconds).IsRequired();
        builder.Property(x => x.ExpiresAtUtc).IsRequired();
        builder.Property(x => x.PollCount).IsRequired();

        builder.Property(x => x.CodeChallenge).HasMaxLength(200);
        builder.Property(x => x.CodeChallengeMethod).HasMaxLength(20);

        builder.Property(x => x.DeviceMetadata).HasMaxLength(500);
        builder.HasIndex(x => x.DeviceCodeHash).IsUnique();
        builder.HasIndex(x => x.UserCodeHash);

        builder.HasIndex(x => new { x.TenantId, x.ClientId });
        builder.HasIndex(x => x.ExpiresAtUtc);
    }
}
