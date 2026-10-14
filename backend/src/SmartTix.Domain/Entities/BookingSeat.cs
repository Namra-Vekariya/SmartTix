using SmartTix.Domain.Common;

namespace SmartTix.Domain.Entities;

public class BookingSeat : BaseEntity
{
    public decimal PriceAtBooking { get; set; }   // Snapshot — price when booked, not current price

    // Foreign keys
    public Guid BookingId { get; set; }
    public Guid SeatId { get; set; }

    // Navigation properties
    public Booking Booking { get; set; } = null!;
    public Seat Seat { get; set; } = null!;
}