namespace SmartTix.Application.DTOs.Ai;

public class AiRecommendResponse
{
    // ok | out_of_scope | no_results — lets the UI distinguish a refusal
    // or an empty match from a real failure without parsing Summary text
    public string Status { get; set; } = AiResponseStatus.Ok;

    // e.g. "Found 3 options for 4 seats together near the centre"
    public string Summary { get; set; } = string.Empty;

    public List<SeatRecommendation> Recommendations { get; set; } = new();
}

public static class AiResponseStatus
{
    public const string Ok = "ok";
    public const string OutOfScope = "out_of_scope";
    public const string NoResults = "no_results";
}

public class SeatRecommendation
{
    // e.g. ["A-1", "A-2", "A-3"]
    public List<string> SeatCodes { get; set; } = new();

    // e.g. "3 together near centre, mid-hall, Premium ₹1,700 each (₹5,100 total)"
    public string Reason { get; set; } = string.Empty;

    // Total price for this option
    public decimal TotalPrice { get; set; }

    // Category name for these seats
    public string CategoryName { get; set; } = string.Empty;
}
