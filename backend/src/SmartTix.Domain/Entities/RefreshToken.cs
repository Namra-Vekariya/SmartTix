using SmartTix.Domain.Common;

namespace SmartTix.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; } = false;
    public DateTime? RevokedAt { get; set; }

    // Foreign key
    public Guid UserId { get; set; }

    // Navigation property
    public User User { get; set; } = null!;
}