namespace SmartTix.Application.Common;

// Upstream LLM provider failed / timed out / returned garbage.
// Mapped to 503 Service Unavailable by the API exception middleware —
// never a 400, because the customer's request itself was valid.
public sealed class AiServiceUnavailableException : Exception
{
    public AiServiceUnavailableException(string message)
        : base(message) { }

    public AiServiceUnavailableException(string message, Exception innerException)
        : base(message, innerException) { }
}
