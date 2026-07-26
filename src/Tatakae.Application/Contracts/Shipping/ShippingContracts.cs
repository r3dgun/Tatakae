using System.ComponentModel.DataAnnotations;

namespace Tatakae.Application.Contracts.Shipping;

public sealed class ShippingQuoteRequest
{
    [Required, StringLength(60)]
    public string Province { get; set; } = string.Empty;

    [Required, StringLength(60)]
    public string City { get; set; } = string.Empty;

    [Range(1, 50)]
    public int ItemCount { get; set; } = 1;

    [Range(typeof(decimal), "0", "999999999")]
    public decimal CartSubtotal { get; set; }
}

public sealed class UpsertManualShippingMethodRequest : IValidatableObject
{
    [Required, StringLength(60, MinimumLength = 2)]
    [RegularExpression("^[a-z0-9-]+$", ErrorMessage = "کد روش ارسال فقط حروف کوچک انگلیسی، عدد و خط تیره باشد.")]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(160, MinimumLength = 2)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(600, MinimumLength = 3)]
    public string Description { get; set; } = string.Empty;

    [Range(0, 999999999)]
    public decimal BasePrice { get; set; }

    [Range(0, 999999999)]
    public decimal? FreeShippingThreshold { get; set; }

    [Range(0, 30)]
    public int EstimatedMinDays { get; set; }

    [Range(0, 60)]
    public int EstimatedMaxDays { get; set; }

    public bool SupportsCashOnDelivery { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;

    [Range(0, 9999, ErrorMessage = "ترتیب نمایش باید بین ۰ تا ۹۹۹۹ باشد.")]
    public int SortOrder { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EstimatedMaxDays < EstimatedMinDays)
        {
            yield return new ValidationResult("حداکثر زمان ارسال نمی‌تواند کمتر از حداقل زمان باشد.", [nameof(EstimatedMinDays), nameof(EstimatedMaxDays)]);
        }

        if (FreeShippingThreshold.HasValue && FreeShippingThreshold.Value > 0 && FreeShippingThreshold.Value < BasePrice)
        {
            yield return new ValidationResult("حداقل مبلغ ارسال رایگان نباید از هزینه پایه ارسال کمتر باشد.", [nameof(FreeShippingThreshold)]);
        }
    }
}

public sealed record ShippingMethodDto(
    Guid Id,
    string Code,
    string Title,
    string Description,
    decimal Price,
    decimal BasePrice,
    decimal? FreeShippingThreshold,
    int EstimatedMinDays,
    int EstimatedMaxDays,
    bool SupportsCashOnDelivery,
    bool IsDefault,
    bool IsActive,
    bool IsAvailable);

public sealed record ShippingMethodSummaryDto(string Code, string Title, string Description, decimal Price, int EstimatedMinDays, int EstimatedMaxDays, bool SupportsCashOnDelivery, bool IsDefault);

public sealed record ShipmentDto(Guid OrderId, string OrderNumber, string Status, string? TrackingCode, string? CarrierName, DateTimeOffset? ShippedAt, DateTimeOffset? DeliveredAt);
