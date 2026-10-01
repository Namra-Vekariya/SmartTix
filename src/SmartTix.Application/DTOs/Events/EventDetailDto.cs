namespace SmartTix.Application.DTOs.Events;

public class EventDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ArtistName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Genre { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public string VenueCity { get; set; } = string.Empty;
    public string? VenueAddress { get; set; }
    public DateTime EventDate { get; set; }
    public DateTime? DoorsOpenTime { get; set; }
    public string? PosterImageUrl { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // Full category breakdown for the ticket pricing table
    public List<SeatCategoryDto> Categories { get; set; } = new();

    // Summary counts
    public int TotalSeatsCount { get; set; }
    public int AvailableSeatsCount { get; set; }
}

public class SeatCategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? ColorHex { get; set; }
    public int TotalSeats { get; set; }
    public int AvailableSeats { get; set; }
}