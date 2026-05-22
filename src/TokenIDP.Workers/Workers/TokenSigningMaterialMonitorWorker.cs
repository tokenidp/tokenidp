using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Foundation.Options;
using TokenIDP.Core.Foundation.Security;
using TokenIDP.Domain.DomainEvents.Activities;
using TokenIDP.Domain.ReadModels.Enums;

namespace TokenIDP.Workers.Workers;

internal sealed class TokenSigningMaterialMonitorWorker : BackgroundService
{
    private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan PollInterval = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TokenOptions _tokenOptions;
    private readonly IAppLogger<TokenSigningMaterialMonitorWorker> _logger;
    private readonly string _workerId = $"{Environment.MachineName}:SigningMaterial:{Guid.NewGuid():N}";

    private string? _lastMaterialId;
    private readonly HashSet<string> _expiredCertificateReports = new(StringComparer.OrdinalIgnoreCase);

    public TokenSigningMaterialMonitorWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<TokenOptions> tokenOptions,
        IAppLogger<TokenSigningMaterialMonitorWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _tokenOptions = tokenOptions.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(InitialDelay, stoppingToken);

        _logger.LogInfo("TokenSigningMaterialMonitorWorker started. WorkerId={WorkerId}", _workerId);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await InspectSigningMaterialAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token signing material monitor failed");
                await RecordWorkerFailureAsync(ex, stoppingToken);
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task InspectSigningMaterialAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var currentMaterialId = TokenSigningMaterialResolver.HasCertificateConfiguration(_tokenOptions)
            ? InspectCertificate(db)
            : InspectKeyMaterial();

        if (_lastMaterialId is not null &&
            !string.Equals(_lastMaterialId, currentMaterialId, StringComparison.Ordinal))
        {
            db.AddDomainEvent(new ActivityDomainEvent(
                TenantId: 0,
                EventType: ActivityEventType.SigningKeyRotated,
                AggregateType: "SigningMaterial",
                AggregateId: currentMaterialId,
                ActorId: null,
                ActorDisplayName: null,
                TargetId: currentMaterialId,
                TargetDescription: "Token signing material",
                Status: "Rotated",
                Description: "Token signing material changed."));
        }

        _lastMaterialId = currentMaterialId;

        await db.SaveChangesAsync(ct);
    }

    private string InspectCertificate(ApplicationDbContext db)
    {
        var certificate = TokenSigningMaterialResolver.LoadCertificate(_tokenOptions);
        var materialId = $"cert:{certificate.Thumbprint}";

        if (certificate.NotAfter <= DateTime.UtcNow &&
            _expiredCertificateReports.Add(materialId))
        {
            db.AddDomainEvent(new ActivityDomainEvent(
                TenantId: 0,
                EventType: ActivityEventType.CertificateExpired,
                AggregateType: "SigningCertificate",
                AggregateId: certificate.Thumbprint,
                ActorId: null,
                ActorDisplayName: null,
                TargetId: certificate.Thumbprint,
                TargetDescription: certificate.Subject,
                Status: "Expired",
                Description: $"Token signing certificate expired at {certificate.NotAfter:u}."));
        }

        return materialId;
    }

    private string InspectKeyMaterial()
    {
        if (!string.IsNullOrWhiteSpace(_tokenOptions.KeyPath))
        {
            var keyInfo = new FileInfo(_tokenOptions.KeyPath);
            return $"key-file:{Hash(_tokenOptions.KeyPath)}:{keyInfo.LastWriteTimeUtc.Ticks}";
        }

        return $"key:{Hash(TokenSigningMaterialResolver.ResolveKeyMaterial(_tokenOptions))}";
    }

    private async Task RecordWorkerFailureAsync(Exception exception, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        BackgroundJobActivityEvents.RaiseFailure(
            db,
            "TokenSigningMaterialMonitorWorker",
            _workerId,
            exception);

        await db.SaveChangesAsync(ct);
    }

    private static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }
}
