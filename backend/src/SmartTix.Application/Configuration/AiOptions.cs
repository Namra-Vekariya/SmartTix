using System.ComponentModel.DataAnnotations;

namespace SmartTix.Application.Configuration;

public sealed class AiOptions
{
    public const string SectionName = "Gemini";

    [Required(ErrorMessage = "Gemini ApiKey is required.")]
    public string ApiKey { get; set; } = string.Empty;

    // gemini-3.5-flash verified working with responseSchema — override to pin another model
    public string Model { get; set; } = "gemini-3.5-flash";

    [Url(ErrorMessage = "Gemini BaseUrl must be a valid URL.")]
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com";

    [Range(5, 120, ErrorMessage = "AI TimeoutSeconds must be between 5 and 120.")]
    public int TimeoutSeconds { get; set; } = 30;
}
