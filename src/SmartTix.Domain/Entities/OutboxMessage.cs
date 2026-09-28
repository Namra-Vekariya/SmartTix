using SmartTix.Domain.Common;
using SmartTix.Domain.Enums;

namespace SmartTix.Domain.Entities;

public class OutboxMessage : BaseEntity
{
    public string EventType { get; set; } = string.Empty;   // "BookingConfirmed", "BookingCancelled"
    public string Payload { get; set; } = string.Empty;     // JSON string
    public OutboxMessageStatus Status { get; set; } = OutboxMessageStatus.Pending;
    public DateTime? ProcessedAt { get; set; }
    public int RetryCount { get; set; } = 0;
    public string? ErrorMessage { get; set; }               // Last error if failed
}