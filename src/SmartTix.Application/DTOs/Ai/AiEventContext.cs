namespace SmartTix.Application.DTOs.Ai;

// Grounding data attached to every prompt so the model can only
// reference rows, categories and prices that actually exist for the event.
public class AiEventContext
{
    public Guid EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string EventDate { get; set; } = string.Empty;   // yyyy-MM-dd
    public List<string> RowLabels { get; set; } = new();
    public List<AiCategoryInfo> Categories { get; set; } = new();
    public int AvailableSeatsCount { get; set; }
}

public class AiCategoryInfo
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
