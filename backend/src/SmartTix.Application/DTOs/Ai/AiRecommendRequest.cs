using System.ComponentModel.DataAnnotations;

namespace SmartTix.Application.DTOs.Ai;

public class AiRecommendRequest
{
    [Required]
    public Guid EventId { get; set; }

    [Required]
    [MinLength(5, ErrorMessage = "Please describe what you are looking for")]
    [MaxLength(500, ErrorMessage = "Query too long — keep it under 500 characters")]
    public string Query { get; set; } = string.Empty;
}