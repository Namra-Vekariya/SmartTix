namespace SmartTix.Domain.Enums;

public enum BookingStatus
{
    Pending = 0,     // Hold created, payment not done
    Confirmed = 1,   // Payment successful
    Cancelled = 2,   // User cancelled
    Refunded = 3,    // Admin issued refund
    Expired = 4      // 10-minute hold timer ran out
}