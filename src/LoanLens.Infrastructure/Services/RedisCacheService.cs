using System.Text.Json;
using LoanLens.Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LoanLens.Infrastructure.Services;

public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache? _distributedCache;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<RedisCacheService> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public RedisCacheService(
        IMemoryCache memoryCache,
        ILogger<RedisCacheService> logger,
        IDistributedCache? distributedCache = null)
    {
        _memoryCache = memoryCache;
        _logger = logger;
        _distributedCache = distributedCache;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        // 1. Try Redis first if available
        if (_distributedCache != null)
        {
            try
            {
                var cachedData = await _distributedCache.GetStringAsync(key, ct);
                if (!string.IsNullOrEmpty(cachedData))
                {
                    return JsonSerializer.Deserialize<T>(cachedData, JsonOptions);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis GET failed for key {Key}, falling back to in-memory cache.", key);
            }
        }

        // 2. Fallback to in-memory cache
        if (_memoryCache.TryGetValue(key, out T? inMemValue))
        {
            return inMemValue;
        }

        return default;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default)
    {
        var timeToLive = expiration ?? TimeSpan.FromHours(1);
        var serialized = JsonSerializer.Serialize(value, JsonOptions);

        // 1. Always set in-memory for resilience
        _memoryCache.Set(key, value, timeToLive);

        // 2. Set in Redis if available
        if (_distributedCache != null)
        {
            try
            {
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = timeToLive
                };
                await _distributedCache.SetStringAsync(key, serialized, options, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis SET failed for key {Key}. Stored in in-memory cache instead.", key);
            }
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        _memoryCache.Remove(key);

        if (_distributedCache != null)
        {
            try
            {
                await _distributedCache.RemoveAsync(key, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis REMOVE failed for key {Key}.", key);
            }
        }
    }
}
