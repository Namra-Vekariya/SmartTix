namespace SmartTix.Application.DTOs.Bookings;

public class BookingDetailDto
{
    public Guid Id { get; set; }

    // Human-readable reference — shown on ticket and confirmation email
    public string BookingReference { get; set; } = string.Empty;  // "SMX-2025-00423"

    public string Status { get; set; } = string.Empty;            // "Confirmed", "Cancelled"
    public string PaymentStatus { get; set; } = string.Empty;     // "Paid", "Pending"

    // Snapshot of total paid — never recalculate from current prices
    // Prices may change after booking
    public decimal TotalAmount { get; set; }

    public DateTime BookedAt { get; set; }
    public DateTime ExpiresAt { get; set; }

    // Event summary — enough for booking history card
    public BookingEventDto Event { get; set; } = null!;

    // Booker details snapshot
    public string BookerName { get; set; } = string.Empty;
    public string BookerEmail { get; set; } = string.Empty;
    public string BookerPhone { get; set; } = string.Empty;

    // Seats in this booking
    public List<BookingSeatDetailDto> Seats { get; set; } = new();
}

public class BookingEventDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ArtistName { get; set; } = string.Empty;
    public string VenueName { get; set; } = string.Empty;
    public string VenueCity { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string? PosterImageUrl { get; set; }
}

public class BookingSeatDetailDto
{
    public Guid SeatId { get; set; }
    public string SeatCode { get; set; } = string.Empty;
    public string RowLabel { get; set; } = string.Empty;
    public int SeatNumber { get; set; }
    public string CategoryName { get; set; } = string.Empty;

    // Price at time of booking — not current price
    // Critical: if event price changes later, this stays as what user paid
    public decimal PriceAtBooking { get; set; }
}