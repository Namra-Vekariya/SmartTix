namespace SmartTix.Application.DTOs.Ai;

// Structured output the LLM must return for a seat request.
// The schema is enforced on the wire via Gemini responseSchema —
// this type is the .NET mirror of that contract.
public class AiSeatQuery
{
    // false when the customer asked something outside seat selection
    // (trivia, coding, opinions, other events...)
    public bool InScope { get; set; } = true;

    // One-line reason when InScope == false — for server logs only
    public string? OutOfScopeReason { get; set; }

    // 1–10, clamped by the seat finder
    public int SeatCount { get; set; } = 1;

    // "under ₹5000 in total"
    public decimal? MaxTotalBudget { get; set; }

    // "less than ₹1500 per seat"
    public decimal? MaxPerSeatBudget { get; set; }

    // front | center | back | any
    public string Position { get; set; } = "any";

    // Seats must be adjacent in one row (default true for groups)
    public bool Together { get; set; } = true;

    // "give me the cheapest option"
    public bool PreferCheapest { get; set; }

    // Only set when the customer names a row that exists — e.g. "D"
    public string? RowLabel { get; set; }

    // Only set when the customer names a category that exists — e.g. "VIP"
    public string? CategoryName { get; set; }
}
