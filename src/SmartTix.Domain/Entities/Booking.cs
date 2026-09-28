using SmartTix.Domain.Common;
using SmartTix.Domain.Enums;

namespace SmartTix.Domain.Entities;

public class Booking : BaseEntity
{
    public string BookingReference { get; set; } = string.Empty;  // "SMX-2025-00423"
    public BookingStatus Status { get; set; } = BookingStatus.Pending;
    public decimal TotalAmount { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public string? PaymentReference { get; set; }   // Mock payment ID
    public DateTime ExpiresAt { get; set; }         // CreatedAt + 10 minutes
    public string? HoldId { get; set; }             // Redis hold key reference

    // Foreign keys
    public Guid UserId { get; set; }
    public Guid EventId { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public Event Event { get; set; } = null!;
    public ICollection<BookingSeat> BookingSeats { get; set; } = new List<BookingSeat>();
}