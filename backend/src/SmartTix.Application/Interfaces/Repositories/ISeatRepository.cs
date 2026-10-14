using SmartTix.Domain.Entities;

namespace SmartTix.Application.Interfaces.Repositories;

public interface ISeatRepository
{
    Task<List<Seat>> GetByEventIdAsync(Guid eventId);
    Task AddRangeAsync(IEnumerable<Seat> seats);
    Task<int> GetAvailableCountByEventIdAsync(Guid eventId);
    Task SaveChangesAsync();
}