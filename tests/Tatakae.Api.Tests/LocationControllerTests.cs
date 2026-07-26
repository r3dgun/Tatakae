using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tatakae.Api.Controllers;
using Microsoft.Extensions.Logging.Abstractions;
using Tatakae.Application.Contracts.Lookups;
using Tatakae.Application.Services;
using Tatakae.Infrastructure.Gateways;
using Tatakae.Infrastructure.Persistence;
using Tatakae.Infrastructure.Persistence.Models;
using Tatakae.Infrastructure.Seeding;

namespace Tatakae.Api.Tests;

public sealed class LocationControllerTests
{
    [Fact]
    public async Task Provinces_ReturnsSeededActiveProvincesWithCities()
    {
        await using var db = CreateDbContext();
        SeedLocations(db);
        var controller = CreateController(db);

        var result = await controller.Provinces(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var provinces = Assert.IsAssignableFrom<IReadOnlyCollection<ProvinceLocationDto>>(ok.Value);
        Assert.Equal(31, provinces.Count);

        var tehran = Assert.Single(provinces, x => x.Name == "تهران");
        Assert.Contains(tehran.Cities, x => x.Name == "تهران" && x.SupportsSameDayDelivery);
        Assert.Contains(tehran.Cities, x => x.Name == "اسلامشهر" && x.SupportsSameDayDelivery);
    }

    [Fact]
    public async Task Cities_WhenProvinceIsProvided_ReturnsOnlyThatProvinceCities()
    {
        await using var db = CreateDbContext();
        SeedLocations(db);
        var controller = CreateController(db);

        var result = await controller.Cities("البرز", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var cities = Assert.IsAssignableFrom<IReadOnlyCollection<CityLocationDto>>(ok.Value);

        Assert.Contains(cities, x => x.Name == "کرج" && x.SupportsSameDayDelivery);
        Assert.Contains(cities, x => x.Name == "فردیس");
        Assert.DoesNotContain(cities, x => x.Name == "تهران");
    }

    [Fact]
    public async Task Cities_WhenProvinceIsEmpty_ReturnsEmptyList()
    {
        await using var db = CreateDbContext();
        SeedLocations(db);
        var controller = CreateController(db);

        var result = await controller.Cities("   ", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var cities = Assert.IsAssignableFrom<IEnumerable<CityLocationDto>>(ok.Value);
        Assert.Empty(cities);
    }

    private static LocationsController CreateController(TatakaeDbContext db)
    {
        var gateway = new EfLocationGateway(db);
        var service = new LocationService(gateway, NullLogger<LocationService>.Instance);
        return new LocationsController(service);
    }

    private static TatakaeDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TatakaeDbContext>()
            .UseInMemoryDatabase($"tatakae-location-tests-{Guid.NewGuid():N}")
            .EnableSensitiveDataLogging()
            .Options;

        return new TatakaeDbContext(options);
    }

    private static void SeedLocations(TatakaeDbContext db)
    {
        foreach (var provinceEntry in IranLocationSeed.ProvincesAndCities)
        {
            var province = new IranianProvinceDbRecord
            {
                Id = Guid.NewGuid(),
                Name = provinceEntry.Key,
                Slug = provinceEntry.Key.Replace(" ", "-"),
                IsActive = true
            };

            db.IranianProvinces.Add(province);

            foreach (var cityName in provinceEntry.Value)
            {
                db.IranianCities.Add(new IranianCityDbRecord
                {
                    Id = Guid.NewGuid(),
                    ProvinceId = province.Id,
                    Name = cityName,
                    Slug = $"{province.Slug}-{cityName.Replace(" ", "-")}",
                    SupportsSameDayDelivery = IsSameDayDelivery(province.Name, cityName),
                    IsActive = true
                });
            }
        }

        db.SaveChanges();
    }

    private static bool IsSameDayDelivery(string provinceName, string cityName) =>
        (provinceName == "تهران" && new[] { "تهران", "ری", "شمیرانات", "اسلامشهر" }.Contains(cityName, StringComparer.Ordinal))
        || (provinceName == "البرز" && cityName == "کرج");
}
