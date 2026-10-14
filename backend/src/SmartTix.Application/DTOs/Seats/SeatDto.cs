namespace SmartTix.Application.DTOs.Seats;

public class SeatDto
{
    public Guid Id { get; set; }
    public string RowLabel { get; set; } = string.Empty;
    public int SeatNumber { get; set; }
    public string SeatCode { get; set; } = string.Empty;   // "A-12"
    public string Status { get; set; } = string.Empty;     // "Available", "Held", "Booked"
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? ColorHex { get; set; }
}

public class SeatMapDto
{
    public Guid EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public List<SeatCategoryInfo> Categories { get; set; } = new();
    public List<SeatDto> Seats { get; set; } = new();

    // Tells Angular how to render the grid
    public int NumberOfRows { get; set; }
    public int SeatsPerRow { get; set; }
}

public class SeatCategoryInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? ColorHex { get; set; }
}