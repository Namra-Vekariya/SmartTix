namespace SmartTix.Domain.Enums;

public enum SeatStatus
{
    Available = 0,
    Held = 1,        // Someone is in checkout — Redis lock active
    Booked = 2,      // Payment confirmed
    Blocked = 3      // Admin manually blocked (e.g., broken seat, staff seat)
}