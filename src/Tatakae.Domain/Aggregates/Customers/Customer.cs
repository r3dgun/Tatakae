using Tatakae.Domain.Common;

namespace Tatakae.Domain.Entities;

/// <summary>Customer aggregate. Browser identity and authentication do not belong to this model.</summary>
public sealed class Customer
{
    private readonly List<Address> _addresses = [];

    private Customer(
        Guid id,
        string fullName,
        string mobile,
        string? email,
        DateTimeOffset createdAt,
        IReadOnlyCollection<Address> addresses)
    {
        Id = DomainGuard.NotEmpty(id, nameof(id), "شناسه مشتری معتبر نیست.");
        FullName = DomainGuard.Required(fullName, nameof(fullName), "نام مشتری الزامی است.");
        Mobile = DomainGuard.Required(mobile, nameof(mobile), "شماره موبایل مشتری الزامی است.");
        Email = DomainGuard.Optional(email);
        CreatedAt = createdAt;
        ReplaceAddresses(addresses);
    }


    public Guid Id { get; }
    public string FullName { get; private set; }
    public string Mobile { get; private set; }
    public string? Email { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public IReadOnlyCollection<Address> Addresses => _addresses.AsReadOnly();

    public static Customer Create(
        Guid id,
        string fullName,
        string mobile,
        string? email,
        DateTimeOffset createdAt)
        => new(id, fullName, mobile, email, createdAt, []);

    public static Customer Rehydrate(
        Guid id,
        string fullName,
        string mobile,
        string? email,
        DateTimeOffset createdAt,
        IReadOnlyCollection<Address> addresses)
        => new(id, fullName, mobile, email, createdAt, addresses);

    public void UpdateProfile(string fullName, string mobile, string? email)
    {
        FullName = DomainGuard.Required(fullName, nameof(fullName), "نام مشتری الزامی است.");
        Mobile = DomainGuard.Required(mobile, nameof(mobile), "شماره موبایل مشتری الزامی است.");
        Email = DomainGuard.Optional(email);
    }

    public void AddOrReplaceAddress(Address address)
    {
        ArgumentNullException.ThrowIfNull(address);

        var index = _addresses.FindIndex(x => x.Id == address.Id);
        if (address.IsDefault)
            ClearDefaultAddress();

        if (index >= 0)
            _addresses[index] = address;
        else
            _addresses.Add(address);

        EnsureOneDefaultAddress();
    }

    public bool RemoveAddress(Guid addressId)
    {
        var address = _addresses.SingleOrDefault(x => x.Id == addressId);
        if (address is null)
            return false;

        var wasDefault = address.IsDefault;
        _addresses.Remove(address);

        if (wasDefault)
            EnsureOneDefaultAddress();

        return true;
    }

    public void SetDefaultAddress(Guid addressId)
    {
        if (_addresses.All(x => x.Id != addressId))
            throw new KeyNotFoundException("آدرس موردنظر برای مشتری پیدا نشد.");

        for (var index = 0; index < _addresses.Count; index++)
            _addresses[index] = _addresses[index].AsDefault(_addresses[index].Id == addressId);
    }

    private void ReplaceAddresses(IEnumerable<Address>? addresses)
    {
        _addresses.Clear();
        foreach (var address in addresses ?? [])
            AddOrReplaceAddress(address);
    }

    private void ClearDefaultAddress()
    {
        for (var index = 0; index < _addresses.Count; index++)
            _addresses[index] = _addresses[index].AsDefault(false);
    }

    private void EnsureOneDefaultAddress()
    {
        if (_addresses.Count == 0 || _addresses.Any(x => x.IsDefault))
            return;

        _addresses[0] = _addresses[0].AsDefault();
    }
}
