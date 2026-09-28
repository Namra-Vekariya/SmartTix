namespace SmartTix.Domain.Enums;

public enum EventStatus
{
    Draft = 0,       // Admin created but not published
    Published = 1,   // Visible to customers
    Ongoing = 2,     // Event happening right now
    Completed = 3,   // Event finished
    Cancelled = 4    // Cancelled — soft deleted but kept for records
}