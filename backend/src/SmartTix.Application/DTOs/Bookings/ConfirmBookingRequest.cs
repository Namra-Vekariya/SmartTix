using System.ComponentModel.DataAnnotations;

namespace SmartTix.Application.DTOs.Bookings;

public class ConfirmBookingRequest
{
    [Required]
    public Guid HoldId { get; set; }

    // Booker details — pre-filled from profile but user can edit on checkout
    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(15)]
    public string Phone { get; set; } = string.Empty;

    // Mock payment details — realistic UI only
    // NEVER stored in database — only used to simulate payment processing
    [Required]
    public MockPaymentDetails Payment { get; set; } = null!;
}

public class MockPaymentDetails
{
    // Fake card details — validated for format only, never stored, never charged
    [Required]
    [RegularExpression(@"^\d{16}$", ErrorMessage = "Card number must be 16 digits")]
    public string CardNumber { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^(0[1-9]|1[0-2])\/\d{2}$", ErrorMessage = "Expiry must be MM/YY format")]
    public string Expiry { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^\d{3,4}$", ErrorMessage = "CVV must be 3 or 4 digits")]
    public string Cvv { get; set; } = string.Empty;
}