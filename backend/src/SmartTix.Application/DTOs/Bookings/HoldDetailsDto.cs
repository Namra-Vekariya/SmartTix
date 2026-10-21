namespace SmartTix.Application.DTOs.Bookings;

// rebuild itself after a browser refresh without trusting client-only state.
public class HoldDetailsDto
{
    public Guid HoldId { get; set; }
    public Guid BookingId { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public decimal TotalAmount { get; set; }
    public List<HeldSeatDto> Seats { get; set; } = new();
}
