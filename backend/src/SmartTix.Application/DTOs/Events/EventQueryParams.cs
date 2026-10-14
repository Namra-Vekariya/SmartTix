using System.ComponentModel.DataAnnotations;

namespace SmartTix.Application.DTOs.Events;

public class EventQueryParams
{
    // Pagination
    [Range(1, int.MaxValue, ErrorMessage = "Page must be at least 1")]
    public int Page { get; set; } = 1;

    [Range(1, 50, ErrorMessage = "PageSize must be between 1 and 50")]
    public int PageSize { get; set; } = 20;

    // Filters
    public string? City { get; set; }
    public string? Genre { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }

    // Search
    public string? Search { get; set; }  // searches event name + artist name

    // Sorting
    public string SortBy { get; set; } = "date";       // date | price | popularity
    public string SortDirection { get; set; } = "asc";  // asc | desc
}