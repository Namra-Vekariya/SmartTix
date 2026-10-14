using SmartTix.Domain.Common;
using SmartTix.Domain.Enums;

namespace SmartTix.Domain.Entities;

public class Event : BaseEntity
{
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
    public EventStatus Status { get; set; } = EventStatus.Draft;

    // Foreign key
    public Guid CreatedByUserId { get; set; }

    // Navigation properties
    public User CreatedBy { get; set; } = null!;
    public ICollection<Seat> Seats { get; set; } = new List<Seat>();
    public ICollection<SeatCategory> SeatCategories { get; set; } = new List<SeatCategory>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}