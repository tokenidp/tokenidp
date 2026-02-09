using IDP.Domain.AggregateRoots.Emails;
using IDP.Foundation.Abstractions;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Notification.Core;

public partial class NotificationDbContext : DbContext
{
    private readonly IAppLogger<NotificationDbContext> _appLogger;

    public NotificationDbContext(DbContextOptions options,
        IAppLogger<NotificationDbContext> appLogger) : base(options)
    {
        _appLogger = appLogger;
    }

    public DbSet<EmailMessage> EmailMessages { get; set; }
    public DbSet<EmailRecipient> EmailRecipients { get; set; }
    public DbSet<EmailAttachment> EmailAttachments { get; set; }
    public DbSet<EmailDeliveryAttempt> EmailDeliveryAttempts { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    /// <summary>
    /// Save changes in database async
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns>affected rows</returns>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        var result = await base.SaveChangesAsync(cancellationToken);

        return result;
    }
}