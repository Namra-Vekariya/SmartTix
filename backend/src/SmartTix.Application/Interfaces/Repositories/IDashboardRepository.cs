using SmartTix.Application.DTOs.Admin;

namespace SmartTix.Application.Interfaces.Repositories;

// Read-side query repository for the admin dashboard.
// Returns read models (DTOs) directly via projection — never entities.
public interface IDashboardRepository
{
    Task<int> GetPublishedEventsCountAsync();
    Task<int> GetBookingsCountAsync(DateTime fromUtc, DateTime toUtc);
    Task<decimal> GetRevenueSumAsync(DateTime fromUtc, DateTime toUtc);
    Task<int> GetActiveHoldsCountAsync(DateTime nowUtc);
    Task<List<RecentBookingDto>> GetRecentBookingsAsync(int limit);
    Task<List<TopEventDto>> GetTopEventsByRevenueAsync(int limit);
}
