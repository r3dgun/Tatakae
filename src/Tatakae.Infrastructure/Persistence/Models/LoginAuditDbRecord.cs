using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tatakae.Infrastructure.Persistence.Models;

[Table("LoginAudits")]
[Index(nameof(UserId))]
[Index(nameof(Mobile))]
[Index(nameof(SessionKey), IsUnique = true)]
[Index(nameof(LoggedInAt))]
public sealed class LoginAuditDbRecord : BaseEntity<Guid>
{
    public Guid? UserId { get; set; }

    [Required, MaxLength(20)]
    public string Mobile { get; set; } = string.Empty;

    [MaxLength(180)]
    public string? FullName { get; set; }

    [Required, MaxLength(80)]
    public string SessionKey { get; set; } = Guid.NewGuid().ToString("N");

    public bool Succeeded { get; set; }

    [MaxLength(500)]
    public string? FailureReason { get; set; }

    [MaxLength(80)]
    public string? IpAddress { get; set; }

    [MaxLength(700)]
    public string? UserAgent { get; set; }

    public DateTimeOffset LoggedInAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? TokenExpiresAt { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }
    public DateTimeOffset? LogoutAt { get; set; }

    [ForeignKey(nameof(UserId))]
    public ApplicationUserIdentity? User { get; set; }
}
