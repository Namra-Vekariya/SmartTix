using SmartTix.Domain.Common;
using SmartTix.Domain.Enums;

namespace SmartTix.Domain.Entities;

public class Seat : BaseEntity
{
    public string RowLabel { get; set; } = string.Empty;    // "A", "B", "C"
    public int SeatNumber { get; set; }                      // 1, 2, 3...
    public string SeatCode { get; set; } = string.Empty;    // "A-12", "B-05"
    public SeatStatus Status { get; set; } = SeatStatus.Available;

    // Foreign keys
    public Guid EventId { get; set; }
    public Guid SeatCategoryId { get; set; }

    // Navigation properties
    public Event Event { get; set; } = null!;
    public SeatCategory SeatCategory { get; set; } = null!;
    public ICollection<BookingSeat> BookingSeats { get; set; } = new List<BookingSeat>();
}