using Tatakae.Web.Responsive;

namespace Tatakae.Web.Tests;

public sealed class ResponsivePageCatalogTests
{
    [Theory]
    [InlineData("", "home")]
    [InlineData("/", "home")]
    [InlineData("shop", "shop")]
    [InlineData("products", "shop")]
    [InlineData("category/embroidered-tshirts", "category")]
    [InlineData("shop/category/hoodies?sort=newest", "category")]
    [InlineData("product/premium-cotton", "product")]
    [InlineData("products/premium-cotton#reviews", "product")]
    [InlineData("customize/premium-cotton", "studio")]
    [InlineData("checkout", "checkout")]
    [InlineData("payment/123", "checkout")]
    [InlineData("order-success/123", "checkout")]
    [InlineData("login", "login")]
    [InlineData("register?returnUrl=/checkout", "login")]
    [InlineData("account", "account")]
    [InlineData("account/orders", "account")]
    [InlineData("admin", "admin")]
    [InlineData("admin/products/new", "admin")]
    [InlineData("rules", "legal")]
    [InlineData("privacy", "legal")]
    [InlineData("shipping-policy", "legal")]
    [InlineData("pages/راهنمای-سفارش", "legal")]
    [InlineData("unknown", "page")]
    public void Resolve_maps_routes_to_dedicated_mobile_page(string route, string expected)
        => Assert.Equal(expected, ResponsivePageCatalog.Resolve(route));

    [Fact]
    public void Catalog_contains_every_phase_13_screen_family()
    {
        var keys = ResponsivePageCatalog.Pages.Select(x => x.Key).ToHashSet(StringComparer.Ordinal);
        var required = new[] { "home", "shop", "category", "product", "studio", "checkout", "login", "account", "admin", "legal", "page" };

        Assert.All(required, key => Assert.Contains(key, keys));
        Assert.Equal(required.Length, keys.Count);
    }
}
