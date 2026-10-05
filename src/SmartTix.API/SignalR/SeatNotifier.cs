using Microsoft.AspNetCore.SignalR;
using SmartTix.API.Hubs;
using SmartTix.Application.DTOs.Seats;
using SmartTix.Application.Interfaces.Infrastructure;
using Microsoft.Extensions.Logging;

namespace SmartTix.API.SignalR;

/// <summary>
/// SignalR implementation of <see cref="ISeatNotifier"/>. Sends the
/// "seatsChanged" event to every client in the event's group.
/// Failures are swallowed and logged: booking correctness must never depend
/// on realtime delivery (clients recover via the REST seat map anyway).
/// </summary>
public class SeatNotifier : ISeatNotifier
{
    private const string EventName = "seatsChanged";

    private readonly IHubContext<SeatsHub> _hubContext;
    private readonly ILogger<SeatNotifier> _logger;

    public SeatNotifier(
        IHubContext<SeatsHub> hubContext,
        ILogger<SeatNotifier> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifySeatsChangedAsync(SeatsChangedEvent seatChange)
    {
        try
        {
            await _hubContext.Clients
                .Group(SeatsHub.GroupName(seatChange.EventId))
                .SendAsync(EventName, seatChange);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to broadcast seat change for event {EventId}", seatChange.EventId);
        }
    }
}
