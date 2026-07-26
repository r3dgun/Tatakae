using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Tatakae.Domain.Enums;

namespace Tatakae.Infrastructure.Persistence.Models;

[Table("Coupons")]
[Index(nameof(Code), IsUnique = true)]
[Index(nameof(IsActive), nameof(StartsAt), nameof(EndsAt))]
public sealed class CouponDbRecord : BaseEntity<Guid>
{
    [Required, MaxLength(80)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(40)]
    public DiscountType Type { get; set; }

    [Precision(18, 2)]
    public decimal Value { get; set; }

    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset? EndsAt { get; set; }

    [Range(1, int.MaxValue)]
    public int? UsageLimit { get; set; }

    [Range(0, int.MaxValue)]
    public int UsageCount { get; set; }

    [Precision(18, 2)]
    public decimal? MinimumOrderAmount { get; set; }

    public bool IsActive { get; set; } = true;
}
