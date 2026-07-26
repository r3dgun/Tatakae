using Tatakae.Domain.Common;

namespace Tatakae.Domain.Entities;

public sealed record Address
{
    public Address(
        Guid id,
        string recipientName,
        string mobile,
        string province,
        string city,
        string postalCode,
        string addressLine,
        string? plaque,
        string? unit,
        bool isDefault)
    {
        Id = DomainGuard.NotEmpty(id, nameof(id), "شناسه آدرس معتبر نیست.");
        RecipientName = DomainGuard.Required(recipientName, nameof(recipientName), "نام گیرنده الزامی است.");
        Mobile = DomainGuard.Required(mobile, nameof(mobile), "شماره موبایل گیرنده الزامی است.");
        Province = DomainGuard.Required(province, nameof(province), "استان الزامی است.");
        City = DomainGuard.Required(city, nameof(city), "شهر الزامی است.");
        PostalCode = DomainGuard.Required(postalCode, nameof(postalCode), "کد پستی الزامی است.");
        AddressLine = DomainGuard.Required(addressLine, nameof(addressLine), "نشانی پستی الزامی است.");
        Plaque = DomainGuard.Optional(plaque);
        Unit = DomainGuard.Optional(unit);
        IsDefault = isDefault;
    }

    public Guid Id { get; }
    public string RecipientName { get; }
    public string Mobile { get; }
    public string Province { get; }
    public string City { get; }
    public string PostalCode { get; }
    public string AddressLine { get; }
    public string? Plaque { get; }
    public string? Unit { get; }
    public bool IsDefault { get; init; }

    public Address AsDefault(bool isDefault = true)
        => this with { IsDefault = isDefault };
}
