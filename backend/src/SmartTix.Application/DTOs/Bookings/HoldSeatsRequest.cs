using System.ComponentModel.DataAnnotations;

namespace SmartTix.Application.DTOs.Bookings;

public class HoldSeatsRequest
{
    [Required]
    public Guid EventId { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "Select at least 1 seat")]
    [MaxLength(6, ErrorMessage = "Cannot book more than 6 seats at once")]
    public List<Guid> SeatIds { get; set; } = new();
}