namespace SmartTix.Application.DTOs.Admin;

public class DashboardDto
{
    public DashboardStatsDto Stats { get; set; } = new();
    public List<RecentBookingDto> RecentBookings { get; set; } = new();
    public List<TopEventDto> TopEvents { get; set; } = new();
}

public class DashboardStatsDto
{
    public int PublishedEvents { get; set; }

    // Status = Confirmed and ConfirmedAt within today's UTC range
    public int BookingsToday { get; set; }

    public decimal RevenueToday { get; set; }

    // Status = Pending and ExpiresAt in the future (DB source of truth)
    public int ActiveHolds { get; set; }
}

public class RecentBookingDto
{
    public string BookingReference { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string EventName { get; set; } = string.Empty;
    public int SeatCount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;

    // COALESCE(ConfirmedAt, CreatedAt) in UTC — cancelled/refunded rows
    public DateTime BookedAt { get; set; }
}

public class TopEventDto
{
    public Guid EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int ConfirmedBookings { get; set; }
}
