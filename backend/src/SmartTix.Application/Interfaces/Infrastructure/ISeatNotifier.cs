using SmartTix.Application.DTOs.Seats;

namespace SmartTix.Application.Interfaces.Infrastructure;

/// <summary>
/// Pushes seat availability changes to connected clients (SignalR).
/// Implementations must be best-effort: they never throw, because a realtime
/// notification failure must never fail the booking operation that triggered it.
/// </summary>
public interface ISeatNotifier
{
    Task NotifySeatsChangedAsync(SeatsChangedEvent seatChange);
}
