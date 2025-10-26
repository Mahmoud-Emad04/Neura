using Microsoft.EntityFrameworkCore;

namespace Neura.Core.Entities;

[Owned]
public class RefreshTokens
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresOn { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? Revoked { get; set; }
    public bool IsExpired => DateTime.UtcNow >= ExpiresOn;
    public bool IsActive => Revoked == null && !IsExpired;
}