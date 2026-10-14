namespace SmartTix.Application.DTOs.Events;

public class EventListDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ArtistName { get; set; } = string.Empty;
    public string VenueName { get; set; } = string.Empty;
    public string VenueCity { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string? PosterImageUrl { get; set; }
    public string Genre { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    // Computed from seat categories — lowest price across all categories
    public decimal StartingPrice { get; set; }

    // Computed from seats — how many are still available
    public int AvailableSeatsCount { get; set; }

    // Total seats for the event
    public int TotalSeatsCount { get; set; }
}