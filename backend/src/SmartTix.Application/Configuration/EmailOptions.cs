using System.ComponentModel.DataAnnotations;

namespace SmartTix.Application.Configuration;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    [Required(ErrorMessage = "Email Host is required.")]
    public string Host { get; set; } = string.Empty;

    [Range(1, 65535, ErrorMessage = "Email Port must be between 1 and 65535.")]
    public int Port { get; set; }

    [Required(ErrorMessage = "Email Username is required.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email Password is required.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email FromAddress is required.")]
    [EmailAddress(ErrorMessage = "Email FromAddress must be a valid email address.")]
    public string FromAddress { get; set; } = string.Empty;

    public string FromName { get; set; } = string.Empty;
}
