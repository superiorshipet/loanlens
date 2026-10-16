using LoanLens.Application.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LoanLens.Api.Health;

public class RedisHealthCheck : IHealthCheck
{
    private readonly ICacheService _cacheService;

    public RedisHealthCheck(ICacheService cacheService)
    {
        _cacheService = cacheService;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await _cacheService.SetAsync("__health_ping__", "pong", TimeSpan.FromMinutes(1), cancellationToken);
            var ping = await _cacheService.GetAsync<string>("__health_ping__", cancellationToken);

            return ping == "pong"
                ? HealthCheckResult.Healthy("Cache layer (Redis / Fallback) is active and responsive.")
                : HealthCheckResult.Degraded("Cache ping did not return expected value.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Cache health check failed.", ex);
        }
    }
}
