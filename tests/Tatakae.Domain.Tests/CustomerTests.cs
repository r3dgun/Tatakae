using Tatakae.Domain.Entities;

namespace Tatakae.Domain.Tests;

public sealed class CustomerTests
{
    [Fact]
    public void Create_UsesExplicitCreationTime()
    {
        var timestamp = new DateTimeOffset(2026, 2, 1, 8, 0, 0, TimeSpan.Zero);
        var customer = Customer.Create(Guid.NewGuid(), "علی رضایی", "09120000000", null, timestamp);
        Assert.Equal(timestamp, customer.CreatedAt);
    }

    [Fact]
    public void AddOrReplaceAddress_KeepsExactlyOneDefaultAddress()
    {
        var customer = Customer.Create(Guid.NewGuid(), "علی رضایی", "09120000000", null, DateTimeOffset.UnixEpoch);
        customer.AddOrReplaceAddress(Address("خانه", true));
        customer.AddOrReplaceAddress(Address("محل کار", true));

        Assert.Single(customer.Addresses.Where(x => x.IsDefault));
        Assert.Equal("محل کار", customer.Addresses.Single(x => x.IsDefault).RecipientName);
    }

    private static Address Address(string recipient, bool isDefault)
        => new(Guid.NewGuid(), recipient, "09120000000", "تهران", "تهران", "1234567890", "خیابان تست", null, null, isDefault);
}
