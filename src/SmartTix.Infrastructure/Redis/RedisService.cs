using StackExchange.Redis;
using SmartTix.Application.Interfaces.Infrastructure;

namespace SmartTix.Infrastructure.Redis;

public class RedisService : IRedisService
{
    private readonly IDatabase _db;

    public RedisService(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    public async Task<bool> AcquireSeatLockAsync(
        Guid eventId,
        Guid seatId,
        Guid userId,
        int expirySeconds = 600)
    {
        var key = BuildSeatKey(eventId, seatId);
        var value = userId.ToString();
        var expiry = TimeSpan.FromSeconds(expirySeconds);

        // SET key value EX 600 NX(10 Minutes)
        // NX = only set if key does not exist
        // This is atomic — Redis guarantees one winner across all concurrent requests
        return await _db.StringSetAsync(key, value, expiry, When.NotExists);
    }

    public async Task ReleaseSeatLockAsync(Guid eventId, Guid seatId)
    {
        var key = BuildSeatKey(eventId, seatId);
        await _db.KeyDeleteAsync(key);
    }

    public async Task<string?> GetSeatLockOwnerAsync(Guid eventId, Guid seatId)
    {
        var key = BuildSeatKey(eventId, seatId);
        var value = await _db.StringGetAsync(key);
        return value.HasValue ? value.ToString() : null;
    }

    public async Task<bool> CheckRateLimitAsync(
        Guid userId,
        int maxRequests = 5,
        int windowSeconds = 60)
    {
        var key = $"ratelimit:book:{userId}";

        var script = @"
        local count = redis.call('INCR', KEYS[1])
        if count == 1 then
            redis.call('EXPIRE', KEYS[1], ARGV[1])
        end
        return count";

        var result = await _db.ScriptEvaluateAsync(
            script,
            keys: new RedisKey[] { key },
            values: new RedisValue[] { windowSeconds });

        var count = (int)result;
        Console.WriteLine($"[RateLimit] UserId: {userId} | Count: {count} | Allowed: {count <= maxRequests}");

        return count <= maxRequests;
    }

    // ── Private helpers ──────────────────────────────────────

    private static string BuildSeatKey(Guid eventId, Guid seatId)
    {
        // Consistent key format across entire codebase
        return $"seat:hold:{eventId}:{seatId}";
    }
}