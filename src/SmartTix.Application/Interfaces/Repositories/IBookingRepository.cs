using SmartTix.Domain.Entities;

namespace SmartTix.Application.Interfaces.Repositories;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(Guid id);
    Task<Booking?> GetByIdWithDetailsAsync(Guid id);
    Task<Booking?> GetByHoldIdAsync(string holdId);
    Task<List<Booking>> GetByUserIdAsync(Guid userId);
    Task<List<Booking>> GetExpiredHoldsAsync();
    Task AddAsync(Booking booking);
    Task SaveChangesAsync();
}