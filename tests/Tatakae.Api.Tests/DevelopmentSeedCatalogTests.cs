using Tatakae.Infrastructure.Seeding;

namespace Tatakae.Api.Tests;

public sealed class DevelopmentSeedCatalogTests
{
    [Fact]
    public void ProductFixtures_CoverEveryRequiredDevelopmentScenario()
    {
        var products = DevelopmentSeedCatalog.CreateProducts();

        var readyMade = Assert.Single(products.Where(x => x.Id == DevelopmentSeedCatalog.ReadyMadeProductId));
        Assert.False(readyMade.SupportsEmbroidery);
        Assert.True(readyMade.IsReadyMade);
        Assert.True(readyMade.IsInStock);

        var customizable = Assert.Single(products.Where(x => x.Id == DevelopmentSeedCatalog.CustomizableProductId));
        Assert.True(customizable.SupportsEmbroidery);
        Assert.True(customizable.IsInStock);

        var discounted = Assert.Single(products.Where(x => x.Id == DevelopmentSeedCatalog.DiscountedProductId));
        Assert.Contains(discounted.Variants, x => x.SalePrice.HasValue && x.SalePrice < x.RegularPrice);

        var outOfStock = Assert.Single(products.Where(x => x.Id == DevelopmentSeedCatalog.OutOfStockProductId));
        Assert.False(outOfStock.IsInStock);
        Assert.All(outOfStock.Variants, x => Assert.Equal(0, x.StockQuantity));
    }

    [Fact]
    public void ProductFixtures_HaveUniqueStableSlugsSkusAndIds()
    {
        var first = DevelopmentSeedCatalog.CreateProducts().OrderBy(x => x.Id).ToArray();
        var second = DevelopmentSeedCatalog.CreateProducts().OrderBy(x => x.Id).ToArray();

        Assert.Equal(first.Length, first.Select(x => x.Id).Distinct().Count());
        Assert.Equal(first.Length, first.Select(x => x.Slug).Distinct(StringComparer.Ordinal).Count());

        var variants = first.SelectMany(x => x.Variants).ToArray();
        Assert.Equal(variants.Length, variants.Select(x => x.Id).Distinct().Count());
        Assert.Equal(variants.Length, variants.Select(x => x.Sku).Distinct(StringComparer.Ordinal).Count());

        Assert.Equal(first.Select(x => x.Id), second.Select(x => x.Id));
        Assert.Equal(
            first.SelectMany(x => x.Variants).OrderBy(x => x.Sku).Select(x => x.Id),
            second.SelectMany(x => x.Variants).OrderBy(x => x.Sku).Select(x => x.Id));
    }

    [Fact]
    public void CustomerFixture_HasDefaultIranianAddress()
    {
        var customer = Assert.Single(DevelopmentSeedCatalog.CreateCustomers().Where(x => x.Id == DevelopmentSeedCatalog.CustomerId));
        var address = Assert.Single(customer.Addresses);

        Assert.Equal(DevelopmentSeedCatalog.Customer.Mobile, customer.Mobile);
        Assert.Equal(DevelopmentSeedCatalog.CustomerAddressId, address.Id);
        Assert.Equal("تهران", address.Province);
        Assert.Equal("تهران", address.City);
        Assert.Equal("1234567890", address.PostalCode);
        Assert.True(address.IsDefault);
    }

    [Fact]
    public void OrderFixture_IsDeterministicAndReferencesSeedCustomerAndSku()
    {
        var first = Assert.Single(DevelopmentSeedCatalog.CreateOrders());
        var second = Assert.Single(DevelopmentSeedCatalog.CreateOrders());
        var products = DevelopmentSeedCatalog.CreateProducts();
        var line = Assert.Single(first.Lines);
        var product = Assert.Single(products.Where(x => x.Id == line.ProductId));

        Assert.Equal(DevelopmentSeedCatalog.TestOrderId, first.Id);
        Assert.Equal(DevelopmentSeedCatalog.TestOrderNumber, first.OrderNumber);
        Assert.Equal(DevelopmentSeedCatalog.CustomerId, first.CustomerId);
        Assert.Equal(first.Id, second.Id);
        Assert.Equal(first.OrderNumber, second.OrderNumber);
        Assert.Contains(product.Variants, x => x.Id == line.VariantId && x.Sku == line.Sku);
        Assert.True(first.Total > 0);
    }

    [Fact]
    public void Credentials_ContainAdminAndCustomerAccountsWithDistinctMobiles()
    {
        var credentials = DevelopmentSeedCatalog.Credentials;

        Assert.Contains(credentials, x => x.Role == "SuperAdmin");
        Assert.Contains(credentials, x => x.Role == "Customer");
        Assert.Equal(credentials.Count, credentials.Select(x => x.Mobile).Distinct(StringComparer.Ordinal).Count());
        Assert.All(credentials, x => Assert.True(x.Password.Length >= 8));
    }
}
