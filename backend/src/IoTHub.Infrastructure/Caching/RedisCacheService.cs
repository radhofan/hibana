using IoTHub.Application.Interfaces;
using StackExchange.Redis;
using System.Text.Json;

namespace IoTHub.Infrastructure.Caching;

public class RedisCacheService(IConnectionMultiplexer redis) : ICacheService
{
    private readonly IDatabase _db = redis.GetDatabase();

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(value);
        await _db.StringSetAsync(key, json, expiry ?? TimeSpan.FromMinutes(5));
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var val = await _db.StringGetAsync(key);
        return val.IsNullOrEmpty ? default : JsonSerializer.Deserialize<T>(val!);
    }

    public Task RemoveAsync(string key, CancellationToken ct = default) =>
        _db.KeyDeleteAsync(key);

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        var cached = await GetAsync<T>(key, ct);
        if (cached is not null) return cached;
        var value = await factory();
        await SetAsync(key, value, expiry, ct);
        return value;
    }
}
