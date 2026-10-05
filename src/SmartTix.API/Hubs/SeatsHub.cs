using Microsoft.AspNetCore.SignalR;

namespace SmartTix.API.Hubs;

/// <summary>
/// Realtime seat map updates. Clients join one group per event they are
/// viewing: "event:{eventId}". The server then pushes "seatsChanged" to that
/// group whenever seat availability changes (hold, confirm, cancel, expiry).
///
/// Anonymous by design — the seat map is public, so no JWT is required to
/// connect or join. Client methods: JoinEvent(eventId), LeaveEvent(eventId).
/// </summary>
public class SeatsHub : Hub
{
    public static string GroupName(Guid eventId) => $"event:{eventId}";

    public Task JoinEvent(Guid eventId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, GroupName(eventId));
    }

    public Task LeaveEvent(Guid eventId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(eventId));
    }
}
