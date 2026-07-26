using System.ComponentModel.DataAnnotations;

namespace Tatakae.Application.Contracts.Account;

public sealed class RegisterCustomerRequest
{
    [Required(ErrorMessage = "نام و نام خانوادگی الزامی است.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "نام و نام خانوادگی باید بین ۳ تا ۱۰۰ کاراکتر باشد.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "شماره موبایل الزامی است.")]
    [RegularExpression(Tatakae.Application.Validation.IranianValidationPatterns.Mobile, ErrorMessage = "شماره موبایل ایران معتبر نیست.")]
    public string Mobile { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "ایمیل معتبر نیست.")]
    [StringLength(150, ErrorMessage = "ایمیل حداکثر ۱۵۰ کاراکتر است.")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "رمز عبور الزامی است.")]
    [StringLength(80, MinimumLength = 6, ErrorMessage = "رمز عبور حداقل باید ۶ کاراکتر باشد.")]
    public string Password { get; set; } = string.Empty;
}

public sealed class LoginRequest
{
    [Required(ErrorMessage = "شماره موبایل الزامی است.")]
    [RegularExpression(Tatakae.Application.Validation.IranianValidationPatterns.Mobile, ErrorMessage = "شماره موبایل ایران معتبر نیست.")]
    public string Mobile { get; set; } = string.Empty;

    [Required(ErrorMessage = "رمز عبور الزامی است.")]
    [StringLength(80, MinimumLength = 6, ErrorMessage = "رمز عبور باید حداقل ۶ کاراکتر باشد.")]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; } = true;
}

public sealed record AccountSessionDto(
    Guid CustomerId,
    string FullName,
    string Mobile,
    string? Email,
    string Token,
    DateTimeOffset ExpiresAt,
    IReadOnlyCollection<string>? Roles = null,
    IReadOnlyCollection<string>? Permissions = null,
    string SessionKey = "",
    bool RememberMe = true);
public sealed record AccountProfileDto(Guid CustomerId, string FullName, string Mobile, string? Email, DateTimeOffset CreatedAt, int OrderCount, decimal LifetimeValue);

public sealed class UpdateAccountProfileRequest
{
    [Required, StringLength(100, MinimumLength = 3)]
    public string FullName { get; set; } = string.Empty;

    [EmailAddress, StringLength(150)]
    public string? Email { get; set; }
}

public sealed class ChangePasswordRequest
{
    [Required, StringLength(80, MinimumLength = 6)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, StringLength(80, MinimumLength = 6)]
    public string NewPassword { get; set; } = string.Empty;
}

public sealed class CustomerAddressRequest
{
    [Required(ErrorMessage = "نام گیرنده الزامی است."), StringLength(100, MinimumLength = 2, ErrorMessage = "نام گیرنده باید بین ۲ تا ۱۰۰ کاراکتر باشد.")]
    public string RecipientName { get; set; } = string.Empty;

    [Required(ErrorMessage = "شماره موبایل گیرنده الزامی است.")]
    [RegularExpression(Tatakae.Application.Validation.IranianValidationPatterns.Mobile, ErrorMessage = "شماره موبایل ایران معتبر نیست.")]
    public string Mobile { get; set; } = string.Empty;

    [Required(ErrorMessage = "استان الزامی است."), StringLength(60, ErrorMessage = "نام استان حداکثر ۶۰ کاراکتر است.")]
    public string Province { get; set; } = string.Empty;

    [Required(ErrorMessage = "شهر الزامی است."), StringLength(60, ErrorMessage = "نام شهر حداکثر ۶۰ کاراکتر است.")]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "کد پستی الزامی است."), RegularExpression(Tatakae.Application.Validation.IranianValidationPatterns.PostalCode, ErrorMessage = "کد پستی باید ۱۰ رقم باشد.")]
    public string PostalCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "آدرس کامل الزامی است."), StringLength(400, MinimumLength = 10, ErrorMessage = "آدرس باید بین ۱۰ تا ۴۰۰ کاراکتر باشد.")]
    public string AddressLine { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Plaque { get; set; }

    [StringLength(20)]
    public string? Unit { get; set; }

    public bool IsDefault { get; set; }
}

public sealed record CustomerAddressDto(Guid Id, string RecipientName, string Mobile, string Province, string City, string PostalCode, string AddressLine, string? Plaque, string? Unit, bool IsDefault);

/// <summary>
/// Transport-neutral request metadata captured by the API adapter.
/// </summary>
public sealed record ClientRequestMetadata(string? IpAddress, string? UserAgent)
{
    public static ClientRequestMetadata Empty { get; } = new(null, null);
}

/// <summary>
/// Minimal authenticated-session identity passed from the API boundary.
/// Application contracts never depend on ClaimsPrincipal or HttpContext.
/// </summary>
public sealed record AuthenticatedSessionContext(Guid UserId, string? SessionKey);
