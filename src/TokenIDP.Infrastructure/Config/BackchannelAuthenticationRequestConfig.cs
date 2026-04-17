using TokenIDP.Domain.AggregateRoots.Authorization;

namespace TokenIDP.Infrastructure.Config;

internal sealed class BackchannelAuthenticationRequestConfig
    : IEntityTypeConfiguration<BackchannelAuthenticationRequest>
{
    public void Configure(EntityTypeBuilder<BackchannelAuthenticationRequest> builder)
    {
        builder.ToTable("BackchannelAuthenticationRequests");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.ClientId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.UserId);
        builder.Property(x => x.RequestedScopes).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.HintType).HasConversion<byte>().IsRequired();
        builder.Property(x => x.HintValueHash).HasMaxLength(256).IsRequired();
        builder.Property(x => x.SubjectHint).HasMaxLength(200);
        builder.Property(x => x.BindingMessage).HasMaxLength(255);
        builder.Property(x => x.UserCodeHash).HasMaxLength(256);
        builder.Property(x => x.AuthReqIdHash).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Status).HasConversion<byte>().IsRequired();
        builder.Property(x => x.DeliveryMode)
            .HasConversion<byte>()
            .IsRequired();
        builder.Property(x => x.IntervalSeconds).IsRequired();
        builder.Property(x => x.ExpiresAtUtc).IsRequired();
        builder.Property(x => x.ClientNotificationTokenHash).HasMaxLength(256);
        builder.Property(x => x.AcrValues).HasMaxLength(250);
        builder.Property(x => x.ApprovedAcr).HasMaxLength(100);
        builder.Property(x => x.ApprovedAmr).HasMaxLength(250);
        builder.Property(x => x.DenialReason).HasMaxLength(500);
        builder.Property(x => x.PollCount).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.CreatedBy).IsRequired();

        builder.HasIndex(x => x.AuthReqIdHash)
            .IsUnique()
            .HasDatabaseName("IX_BackchannelAuthenticationRequests_AuthReqIdHash");

        builder.HasIndex(x => new { x.TenantId, x.ClientId, x.Status, x.ExpiresAtUtc })
            .HasDatabaseName("IX_BackchannelAuthenticationRequests_Client_Status_Expiry");

        builder.HasIndex(x => new { x.TenantId, x.UserId, x.Status, x.ExpiresAtUtc })
            .HasDatabaseName("IX_BackchannelAuthenticationRequests_User_Status_Expiry");
    }
}
