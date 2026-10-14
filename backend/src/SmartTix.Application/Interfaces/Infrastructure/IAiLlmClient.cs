using SmartTix.Application.DTOs.Ai;

namespace SmartTix.Application.Interfaces.Infrastructure;

// Port for the LLM provider — Application never sees HTTP, URLs or API keys.
// Implemented by GeminiLlmClient in Infrastructure.
public interface IAiLlmClient
{
    Task<AiSeatQuery> ParseSeatQueryAsync(
        string query,
        AiEventContext context,
        CancellationToken cancellationToken = default);
}
