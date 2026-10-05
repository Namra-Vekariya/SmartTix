namespace SmartTix.Application.DTOs.Seats;

/// Payload pushed over SignalR ("seatsChanged" event) whenever seat availability
/// changes for an event. Clients subscribed to group "event:{eventId}" receive it.

public class SeatsChangedEvent
{
    public Guid EventId { get; set; }
    public string Status { get; set; } = string.Empty; //"held" | "confirmed" | "payment_failed" | "cancelled" | "expired"

    public string Reason { get; set; } = string.Empty;

    public List<ChangedSeatDto> Seats { get; set; } = [];

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class ChangedSeatDto
{
    public Guid SeatId { get; set; }
    public string SeatCode { get; set; } = string.Empty;
}
