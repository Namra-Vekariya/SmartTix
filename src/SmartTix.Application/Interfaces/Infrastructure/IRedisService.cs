namespace SmartTix.Application.Interfaces.Infrastructure;

public interface IRedisService
{
    // Returns true if lock acquired, false if seat already held by someone else
    Task<bool> AcquireSeatLockAsync(Guid eventId, Guid seatId, Guid userId, int expirySeconds = 600);

    // Releases lock when payment fails or booking cancelled
    Task ReleaseSeatLockAsync(Guid eventId, Guid seatId);

    // Returns userId who holds the lock, null if no lock exists
    Task<string?> GetSeatLockOwnerAsync(Guid eventId, Guid seatId);

    // Rate limiting — returns true if request is allowed, false if limit exceeded
    Task<bool> CheckRateLimitAsync(Guid userId, int maxRequests = 5, int windowSeconds = 60);
}