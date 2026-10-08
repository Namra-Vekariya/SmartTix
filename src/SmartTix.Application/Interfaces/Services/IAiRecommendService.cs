using SmartTix.Application.DTOs.Ai;

namespace SmartTix.Application.Interfaces.Services;

public interface IAiRecommendService
{
    Task<AiRecommendResponse> RecommendAsync(
        AiRecommendRequest request,
        CancellationToken cancellationToken = default);
}
