using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SmartTix.Application.Common;
using SmartTix.Application.DTOs.Ai;
using SmartTix.Application.Interfaces.Services;

namespace SmartTix.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AiController : ControllerBase
{
    private readonly IAiRecommendService _aiRecommendService;

    public AiController(IAiRecommendService aiRecommendService)
    {
        _aiRecommendService = aiRecommendService;
    }

    // Natural-language seat concierge — public like the seat map it reads,
    // but rate-limited because every call hits a paid LLM provider
    [HttpPost("recommend")]
    [AllowAnonymous]
    [EnableRateLimiting("ai")]
    public async Task<IActionResult> Recommend(
        [FromBody] AiRecommendRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _aiRecommendService.RecommendAsync(request, cancellationToken);

        var message = result.Status switch
        {
            AiResponseStatus.OutOfScope => "Request was outside the assistant's scope",
            AiResponseStatus.NoResults => "No seats matched the request",
            _ => "Seat recommendations generated successfully"
        };

        return Ok(ApiResponse<AiRecommendResponse>.Ok(result, message));
    }
}
