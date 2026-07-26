using Microsoft.Extensions.Logging;
using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Contracts.Lookups;
using Tatakae.Application.Interfaces.Gateways;
using Tatakae.Application.Interfaces.Services;

namespace Tatakae.Application.Services;

public sealed class LocationService(
    ILocationGateway gateway,
    ILogger<LocationService> logger) : ILocationService
{
    public Task<ResultDto<IReadOnlyCollection<ProvinceLocationDto>>> GetProvincesAsync(CancellationToken cancellationToken = default)
        => ApplicationServiceResult.ExecuteAsync(
            () => gateway.GetProvincesAsync(cancellationToken),
            "فهرست استان‌ها با موفقیت دریافت شد.",
            "خطایی در دریافت فهرست استان‌ها رخ داده است.",
            "locations_provinces_get_failed",
            logger);

    public Task<ResultDto<IReadOnlyCollection<CityLocationDto>>> GetCitiesAsync(string province, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(province))
        {
            return Task.FromResult(new ResultDto<IReadOnlyCollection<CityLocationDto>>().Success(
                "نام استان وارد نشده است.",
                Array.Empty<CityLocationDto>()));
        }

        return ApplicationServiceResult.ExecuteAsync(
            () => gateway.GetCitiesAsync(province, cancellationToken),
            "فهرست شهرها با موفقیت دریافت شد.",
            "خطایی در دریافت فهرست شهرها رخ داده است.",
            "locations_cities_get_failed",
            logger);
    }
}
