using System.Globalization;
using SmartTix.Application.DTOs.Ai;
using SmartTix.Application.Interfaces.Infrastructure;
using SmartTix.Application.Interfaces.Repositories;
using SmartTix.Application.Interfaces.Services;
using SmartTix.Domain.Entities;
using SmartTix.Domain.Enums;

namespace SmartTix.Application.Services;

// AI seat concierge pipeline:
//   1. load event + seat grid from the database
//   2. LLM parses the free-text query into structured constraints
//      (or routes it out of scope and we refuse)
//   3. SeatFinder applies those constraints with deterministic code
public sealed class AiRecommendService : IAiRecommendService
{
    private const string OutOfScopeMessage =
        "I can only help you choose seats for this event. " +
        "Try something like \"4 seats together under ₹5000, near the centre\".";

    private readonly IEventRepository _eventRepository;
    private readonly ISeatRepository _seatRepository;
    private readonly IAiLlmClient _llmClient;

    public AiRecommendService(
        IEventRepository eventRepository,
        ISeatRepository seatRepository,
        IAiLlmClient llmClient)
    {
        _eventRepository = eventRepository;
        _seatRepository = seatRepository;
        _llmClient = llmClient;
    }

    public async Task<AiRecommendResponse> RecommendAsync(
        AiRecommendRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.EventId == Guid.Empty)
            throw new ArgumentException("EventId is required", nameof(request));

        if (string.IsNullOrWhiteSpace(request.Query))
            throw new ArgumentException("Query is required", nameof(request));

        var eventEntity = await _eventRepository.GetByIdWithCategoriesAsync(request.EventId);

        if (eventEntity is null)
            throw new KeyNotFoundException($"Event with ID {request.EventId} was not found");

        var seats = await _seatRepository.GetByEventIdAsync(request.EventId);

        var context = BuildContext(eventEntity, seats);
        var query = request.Query.Trim();

        var parsed = await _llmClient.ParseSeatQueryAsync(query, context, cancellationToken);

        // ── Out of scope: refuse with a canned fallback, never answer trivia ──
        if (!parsed.InScope)
        {
            return new AiRecommendResponse
            {
                Status = AiResponseStatus.OutOfScope,
                Summary = OutOfScopeMessage
            };
        }

        var options = SeatFinder.Find(seats, parsed);

        if (options.Count == 0)
        {
            return new AiRecommendResponse
            {
                Status = AiResponseStatus.NoResults,
                Summary = BuildNoResultsMessage(parsed, seats)
            };
        }

        return new AiRecommendResponse
        {
            Status = AiResponseStatus.Ok,
            Summary = BuildOkSummary(parsed, options),
            Recommendations = options
        };
    }

    // ── Private helpers ──────────────────────────────────────

    private static AiEventContext BuildContext(Event eventEntity, List<Seat> seats)
    {
        return new AiEventContext
        {
            EventId = eventEntity.Id,
            EventName = eventEntity.Name,
            EventDate = eventEntity.EventDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            RowLabels = seats
                .Select(s => s.RowLabel)
                .Distinct()
                .OrderBy(r => r, StringComparer.Ordinal)
                .ToList(),
            Categories = eventEntity.SeatCategories
                .Select(sc => new AiCategoryInfo { Name = sc.Name, Price = sc.Price })
                .ToList(),
            AvailableSeatsCount = seats.Count(s =>
                s.Status == SeatStatus.Available)
        };
    }

    private static string BuildOkSummary(AiSeatQuery query, List<SeatRecommendation> options)
    {
        var cheapest = options.Min(o => o.TotalPrice);
        var seatWord = query.SeatCount == 1 ? "seat" : "seats";
        var budgetHint = query.MaxTotalBudget is > 0
            ? $" within a {FormatMoney(query.MaxTotalBudget.Value)} budget"
            : query.MaxPerSeatBudget is > 0
                ? $" under {FormatMoney(query.MaxPerSeatBudget.Value)} per seat"
                : string.Empty;

        return $"Found {options.Count} option(s) for {query.SeatCount} {seatWord}{budgetHint} " +
               $"— best pick from {FormatMoney(cheapest)} total.";
    }

    private static string BuildNoResultsMessage(AiSeatQuery query, List<Seat> seats)
    {
        var availableCount = seats.Count(s => s.Status == SeatStatus.Available);

        if (availableCount == 0)
            return "This event is fully booked right now — no seats are available.";

        var seatWord = query.SeatCount == 1 ? "seat" : "seats";
        return $"I couldn't find {query.SeatCount} matching {seatWord} " +
               $"({availableCount} seats are still available). " +
               "Try a higher budget, fewer seats, or a different row.";
    }

    private static string FormatMoney(decimal value) =>
        "₹" + value.ToString("N0", CultureInfo.InvariantCulture);
}
