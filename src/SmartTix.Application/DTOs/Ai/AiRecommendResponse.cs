namespace SmartTix.Application.DTOs.Ai;

public class AiRecommendResponse
{
    // "Found 2 great options matching your preferences"
    public string Summary { get; set; } = string.Empty;

    public List<SeatRecommendation> Recommendations { get; set; } = new();
}

public class SeatRecommendation
{
    // e.g. ["A-1", "A-2", "A-3"]
    public List<string> SeatCodes { get; set; } = new();

    // e.g. "Front row center, 3 consecutive seats, ₹5000 each"
    public string Reason { get; set; } = string.Empty;

    // Total price for this option
    public decimal TotalPrice { get; set; }

    // Category name for these seats
    public string CategoryName { get; set; } = string.Empty;
}