using LoanLens.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LoanLens.Api.Health;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly LoanLensDbContext _db;

    public DatabaseHealthCheck(LoanLensDbContext db)
    {
        _db = db;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _db.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("Database is online and responsive.")
                : HealthCheckResult.Unhealthy("Unable to establish connection to the database.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database check failed with exception.", ex);
        }
    }
}
