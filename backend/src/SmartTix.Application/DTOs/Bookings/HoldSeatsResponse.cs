namespace SmartTix.Application.DTOs.Bookings;

public class HoldSeatsResponse
{
    // Unique ID for this hold session — passed to checkout page
    public Guid HoldId { get; set; }

    public Guid EventId { get; set; }
    public string EventName { get; set; } = string.Empty;

    // When the hold expires — Angular uses this for the countdown timer
    public DateTime ExpiresAt { get; set; }

    // Total amount pre-calculated — no extra API call needed on checkout page
    public decimal TotalAmount { get; set; }

    // Held seats with enough detail for the checkout summary
    public List<HeldSeatDto> Seats { get; set; } = new();
}

public class HeldSeatDto
{
    public Guid SeatId { get; set; }
    public string SeatCode { get; set; } = string.Empty;      // "A-1", "B-3"
    public string CategoryName { get; set; } = string.Empty;   // "VIP", "Premium"
    public decimal Price { get; set; }
    public string RowLabel { get; set; } = string.Empty;
    public int SeatNumber { get; set; }
}