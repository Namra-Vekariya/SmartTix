using System.ComponentModel.DataAnnotations;
using SmartTix.Domain.Enums;

namespace SmartTix.Application.DTOs.Events;

public class UpdateEventRequest
{
    // All fields optional — admin updates only what changed
    // Seat layout excluded — dangerous to change after tickets sold

    [MaxLength(200)]
    public string? Name { get; set; }

    [MaxLength(100)]
    public string? ArtistName { get; set; }

    public string? Description { get; set; }

    [MaxLength(50)]
    public string? Genre { get; set; }

    [MaxLength(200)]
    public string? VenueName { get; set; }

    [MaxLength(100)]
    public string? VenueCity { get; set; }

    public string? VenueAddress { get; set; }

    public DateTime? EventDate { get; set; }

    public DateTime? DoorsOpenTime { get; set; }

    public string? PosterImageUrl { get; set; }

    public EventStatus? Status { get; set; }
}