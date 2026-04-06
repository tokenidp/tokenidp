namespace TokenIDP.Infrastructure.Config;

internal class ClientAuthPolicyConfig : IEntityTypeConfiguration<ClientAuthPolicy>
{
    public void Configure(EntityTypeBuilder<ClientAuthPolicy> b)
    {
        b.ToTable("ClientAuthPolicies");

        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();

        b.Property(x => x.ClientId).IsRequired();
        b.HasIndex(x => new { x.ClientId }).IsUnique();

        b.Property(x => x.AllowLocalLoginOverride);
        b.Property(x => x.AllowSelfRegistrationOverride);

        b.Property(x => x.MfaPolicyOverride).IsRequired();
        b.Property(x => x.ShowExternalProviders).IsRequired();
        b.Property(x => x.ShowStaySignedIn).IsRequired();
        b.Property(x => x.ShowCreateAccountLink).IsRequired();
        b.Property(x => x.AllowForgotPassword).IsRequired();
        b.Property(x => x.AutoCreateUsers).IsRequired();
        b.Property(x => x.DefaultRoleId);

        b.HasOne(x => x.Client)
         .WithOne(x => x.ClientAuthPolicy)
         .HasForeignKey<ClientAuthPolicy>(x => x.ClientId)
         .OnDelete(DeleteBehavior.NoAction);
    }
}
