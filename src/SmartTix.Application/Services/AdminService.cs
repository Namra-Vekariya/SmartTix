using SmartTix.Application.DTOs.Admin;
using SmartTix.Application.Interfaces.Repositories;
using SmartTix.Application.Interfaces.Services;

namespace SmartTix.Application.Services;

public class AdminService : IAdminService
{
    private const int RecentBookingsLimit = 20;
    private const int TopEventsLimit = 5;

    private readonly IDashboardRepository _dashboardRepository;

    public AdminService(IDashboardRepository dashboardRepository)
    {
        _dashboardRepository = dashboardRepository;
    }

    public async Task<DashboardDto> GetDashboardAsync()
    {
        // "Today" = the current UTC calendar day, half-open [start, next start)
        var nowUtc = DateTime.UtcNow;
        var dayStartUtc = nowUtc.Date;
        var dayEndUtc = dayStartUtc.AddDays(1);

        // Queries run one after another on purpose: they share one scoped
        // DbContext, and a DbContext cannot execute concurrent operations.
        var publishedEvents = await _dashboardRepository.GetPublishedEventsCountAsync();
        var bookingsToday = await _dashboardRepository.GetBookingsCountAsync(dayStartUtc, dayEndUtc);
        var revenueToday = await _dashboardRepository.GetRevenueSumAsync(dayStartUtc, dayEndUtc);
        var activeHolds = await _dashboardRepository.GetActiveHoldsCountAsync(nowUtc);
        var recentBookings = await _dashboardRepository.GetRecentBookingsAsync(RecentBookingsLimit);
        var topEvents = await _dashboardRepository.GetTopEventsByRevenueAsync(TopEventsLimit);

        return new DashboardDto
        {
            Stats = new DashboardStatsDto
            {
                PublishedEvents = publishedEvents,
                BookingsToday = bookingsToday,
                RevenueToday = revenueToday,
                ActiveHolds = activeHolds
            },
            RecentBookings = recentBookings,
            TopEvents = topEvents
        };
    }
}
