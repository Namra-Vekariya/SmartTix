using Microsoft.EntityFrameworkCore;
using SmartTix.Application.DTOs.Admin;
using SmartTix.Application.Interfaces.Repositories;
using SmartTix.Domain.Enums;
using SmartTix.Infrastructure.Data;

namespace SmartTix.Infrastructure.Repositories;

// Read-side query repository for the admin dashboard.
public class DashboardRepository : IDashboardRepository
{
    // Real bookings only — Pending holds and Expired checkouts are not
    // shown in the recent-bookings table (they'd flood it)
    private static readonly BookingStatus[] RecentBookingStatuses =
    {
        BookingStatus.Confirmed,
        BookingStatus.Cancelled,
        BookingStatus.Refunded
    };

    private readonly AppDbContext _context;

    public DashboardRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<int> GetPublishedEventsCountAsync()
    {
        return _context.Events
            .CountAsync(e =>
                e.Status != EventStatus.Draft &&
                e.Status != EventStatus.Cancelled);
    }

    public Task<int> GetBookingsCountAsync(DateTime fromUtc, DateTime toUtc)
    {
        return _context.Bookings
            .CountAsync(b =>
                b.Status == BookingStatus.Confirmed &&
                b.ConfirmedAt >= fromUtc &&
                b.ConfirmedAt < toUtc);
    }

    public Task<decimal> GetRevenueSumAsync(DateTime fromUtc, DateTime toUtc)
    {
        return _context.Bookings
            .Where(b =>
                b.Status == BookingStatus.Confirmed &&
                b.ConfirmedAt >= fromUtc &&
                b.ConfirmedAt < toUtc)
            .SumAsync(b => b.TotalAmount);
    }

    public Task<int> GetActiveHoldsCountAsync(DateTime nowUtc)
    {
        // PostgreSQL is the source of truth, not a Redis scan.
        return _context.Bookings
            .CountAsync(b =>
                b.Status == BookingStatus.Pending &&
                b.ExpiresAt > nowUtc);
    }

    public async Task<List<RecentBookingDto>> GetRecentBookingsAsync(int limit)
    {
        // COALESCE(ConfirmedAt, CreatedAt) is defined once here, in the
        // projection, and the OrderBy reuses the projected BookedAt value.
        // Rows never confirmed (cancelled/refunded) fall back to CreatedAt.
        return await _context.Bookings
            .AsNoTracking()
            .Where(b => RecentBookingStatuses.Contains(b.Status))
            .Select(b => new RecentBookingDto
            {
                BookingReference = b.BookingReference,
                CustomerName = b.BookerName,
                EventName = b.Event != null ? b.Event.Name : string.Empty,
                SeatCount = b.BookingSeats.Count,
                TotalAmount = b.TotalAmount,
                Status = b.Status.ToString(),
                BookedAt = b.ConfirmedAt ?? b.CreatedAt
            })
            .OrderByDescending(x => x.BookedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<TopEventDto>> GetTopEventsByRevenueAsync(int limit)
    {
        return await _context.Events
            .AsNoTracking()
            .Select(e => new TopEventDto
            {
                EventId = e.Id,
                EventName = e.Name,
                Revenue = e.Bookings
                    .Where(b => b.Status == BookingStatus.Confirmed)
                    .Sum(b => (decimal?)b.TotalAmount) ?? 0m,
                ConfirmedBookings = e.Bookings
                    .Count(b => b.Status == BookingStatus.Confirmed)
            })
            .Where(x => x.Revenue > 0)
            .OrderByDescending(x => x.Revenue)
            .Take(limit)
            .ToListAsync();
    }
}
