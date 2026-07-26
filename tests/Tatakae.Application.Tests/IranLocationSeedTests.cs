using Tatakae.Infrastructure.Seeding;

namespace Tatakae.Application.Tests;

public sealed class IranLocationSeedTests
{
    [Fact]
    public void ProvincesAndCities_HasAllIranProvinces()
    {
        var locations = IranLocationSeed.ProvincesAndCities;

        Assert.Equal(31, locations.Count);
        Assert.Contains("تهران", locations.Keys);
        Assert.Contains("اصفهان", locations.Keys);
        Assert.Contains("فارس", locations.Keys);
        Assert.Contains("خراسان رضوی", locations.Keys);
        Assert.Contains("آذربایجان شرقی", locations.Keys);
    }

    [Fact]
    public void ProvincesAndCities_ContainsCheckoutCriticalCities()
    {
        var locations = IranLocationSeed.ProvincesAndCities;

        Assert.Contains("تهران", locations["تهران"]);
        Assert.Contains("ری", locations["تهران"]);
        Assert.Contains("کرج", locations["البرز"]);
        Assert.Contains("مشهد", locations["خراسان رضوی"]);
        Assert.Contains("شیراز", locations["فارس"]);
        Assert.Contains("اصفهان", locations["اصفهان"]);
    }

    [Fact]
    public void ProvincesAndCities_DoesNotContainEmptyOrDuplicateNames()
    {
        var locations = IranLocationSeed.ProvincesAndCities;

        Assert.All(locations.Keys, province => Assert.False(string.IsNullOrWhiteSpace(province)));

        foreach (var province in locations)
        {
            Assert.NotEmpty(province.Value);
            Assert.All(province.Value, city => Assert.False(string.IsNullOrWhiteSpace(city)));
            Assert.Equal(province.Value.Length, province.Value.Distinct(StringComparer.Ordinal).Count());
        }
    }

    [Fact]
    public void ProvincesAndCities_HasEnoughCitiesForProductionCheckout()
    {
        var locations = IranLocationSeed.ProvincesAndCities;
        var cityCount = locations.Values.Sum(x => x.Length);

        Assert.True(cityCount >= 300, $"Expected at least 300 cities, but seed contains {cityCount}.");
    }
}
