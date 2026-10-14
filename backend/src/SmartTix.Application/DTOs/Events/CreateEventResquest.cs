
using System.ComponentModel.DataAnnotations;

namespace SmartTix.Application.DTOs.Events;

public class CreateEventRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ArtistName { get; set; } = string.Empty;

    public string? Description { get; set; }

    [MaxLength(50)]
    public string? Genre { get; set; }

    [Required]
    [MaxLength(200)]
    public string VenueName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string VenueCity { get; set; } = string.Empty;

    public string? VenueAddress { get; set; }

    [Required]
    public DateTime EventDate { get; set; }

    public DateTime? DoorsOpenTime { get; set; }

    public string? PosterImageUrl { get; set; }

    // Seat layout — defines how the venue is structured
    [Required]
    [Range(1, 26, ErrorMessage = "Rows must be between 1 and 26 (A-Z)")]
    public int NumberOfRows { get; set; }

    [Required]
    [Range(1, 50, ErrorMessage = "Seats per row must be between 1 and 50")]
    public int SeatsPerRow { get; set; }

    // Category definitions — which rows belong to which price tier
    [Required]
    [MinLength(1, ErrorMessage = "At least one seat category is required")]
    public List<CreateSeatCategoryRequest> Categories { get; set; } = new();
}

public class CreateSeatCategoryRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;      // "Front Row", "Premium", "General"

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; set; }

    [MaxLength(7)]
    public string? ColorHex { get; set; }                 // "#FF5733"

    // Which row labels belong to this category
    // e.g. ["A", "B"] for Front Row
    [Required]
    [MinLength(1, ErrorMessage = "At least one row must be assigned to a category")]
    public List<string> Rows { get; set; } = new();
}