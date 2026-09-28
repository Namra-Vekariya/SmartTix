using SmartTix.Domain.Common;

namespace SmartTix.Domain.Entities;

public class SeatCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;   // "Front Row", "Premium", "General"
    public decimal Price { get; set; }
    public string? ColorHex { get; set; }               // "#FF5733" for seat map display

    // Foreign key
    public Guid EventId { get; set; }

    // Navigation properties
    public Event Event { get; set; } = null!;
    public ICollection<Seat> Seats { get; set; } = new List<Seat>();
}