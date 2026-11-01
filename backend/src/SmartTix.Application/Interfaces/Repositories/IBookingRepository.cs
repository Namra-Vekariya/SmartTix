using SmartTix.Domain.Entities;

namespace SmartTix.Application.Interfaces.Repositories;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(Guid id);
    Task<Booking?> GetByIdWithDetailsAsync(Guid id);
    Task<Booking?> GetByHoldIdAsync(string holdId);
    Task<List<Booking>> GetByUserIdAsync(Guid userId);
    Task<Booking?> GetActiveHoldAsync(Guid userId, Guid eventId);
    Task<List<Booking>> GetExpiredHoldsAsync(int limit);
    Task<bool> TryExpireBookingAsync(Guid bookingId, DateTime utcNow);
    Task AddAsync(Booking booking);
    Task SaveChangesAsync();
}