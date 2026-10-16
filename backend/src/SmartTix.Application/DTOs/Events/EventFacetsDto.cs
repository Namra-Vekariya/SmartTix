namespace SmartTix.Application.DTOs.Events;

public class EventFacetsDto
{
    public List<string> Genres { get; set; } = new();
    public List<string> Cities { get; set; } = new();
}
