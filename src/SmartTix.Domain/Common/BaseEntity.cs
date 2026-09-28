namespace SmartTix.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Soft delete fields — every entity gets these
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
}