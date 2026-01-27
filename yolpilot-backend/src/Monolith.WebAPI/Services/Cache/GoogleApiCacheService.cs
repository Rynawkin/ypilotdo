using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;
using System.Text;

namespace Monolith.WebAPI.Services.Cache;

public interface IGoogleApiCacheService
{
    Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);
    string GenerateCacheKey(string prefix, params string[] parts);
}

public class GoogleApiCacheService : IGoogleApiCacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<GoogleApiCacheService> _logger;
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromHours(24);

    public GoogleApiCacheService(IMemoryCache cache, ILogger<GoogleApiCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
    {
        // Try to get from cache
        if (_cache.TryGetValue<T>(key, out var cachedValue))
        {
            _logger.LogDebug("Cache HIT for key: {Key}", key);
            return cachedValue;
        }

        _logger.LogDebug("Cache MISS for key: {Key}", key);

        // Not in cache, create new value
        var value = await factory();

        if (value != null)
        {
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? DefaultExpiration,
                Size = 1 // For memory management
            };

            _cache.Set(key, value, cacheOptions);
            _logger.LogInformation("Cached result for key: {Key} (expires in {Expiration})", key, expiration ?? DefaultExpiration);
        }

        return value;
    }

    public string GenerateCacheKey(string prefix, params string[] parts)
    {
        // Create a deterministic hash from the parts
        var combined = string.Join("|", parts);
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(combined));
        var hashString = Convert.ToBase64String(hash).Replace("+", "-").Replace("/", "_").Replace("=", "");
        return $"{prefix}:{hashString}";
    }
}
