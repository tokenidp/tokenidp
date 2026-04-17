namespace TokenIDP.Infrastructure.Config;

internal class UserContactConfig : IEntityTypeConfiguration<UserContact>
{
    public void Configure(EntityTypeBuilder<UserContact> builder)
    {
        builder.ToTable("UserContacts");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Relationship).HasMaxLength(50);
        builder.Property(x => x.ContactType).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(256).IsRequired();
        builder.Property(x => x.PhoneNumber).HasMaxLength(50).IsRequired();

        builder.Property(x => x.AddressLine1).HasMaxLength(250);
        builder.Property(x => x.AddressLine2).HasMaxLength(250);
        builder.Property(x => x.City).HasMaxLength(100);
        builder.Property(x => x.State).HasMaxLength(50);
        builder.Property(x => x.PostalCode).HasMaxLength(20);
        builder.Property(x => x.Country).HasMaxLength(100);
        builder.Property(x => x.IsActive).IsRequired();

        builder.HasOne(u => u.User)
            .WithMany(u => u.UserContacts)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        // Fast lookup for profile load
        builder.HasIndex(x => new { x.UserId, x.IsActive })
            .HasDatabaseName("IX_UserContacts_User_Active");

        // Optional: prevent duplicate same email/phone per user
        builder.HasIndex(x => new { x.UserId, x.Email })
            .IsUnique()
            .HasDatabaseName("IX_UserContacts_User_Email");

        builder.HasIndex(x => new { x.UserId, x.PhoneNumber })
            .IsUnique()
            .HasDatabaseName("IX_UserContacts_User_Phone");
    }
}

