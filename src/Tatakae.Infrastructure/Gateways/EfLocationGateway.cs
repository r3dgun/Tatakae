using Microsoft.EntityFrameworkCore;
using Tatakae.Application.Contracts.Lookups;
using Tatakae.Application.Interfaces.Gateways;
using Tatakae.Infrastructure.Persistence;

namespace Tatakae.Infrastructure.Gateways;

public sealed class EfLocationGateway(TatakaeDbContext db) : ILocationGateway
{
    public async Task<IReadOnlyCollection<ProvinceLocationDto>> GetProvincesAsync(CancellationToken cancellationToken = default)
        => await QueryLocations().ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<CityLocationDto>> GetCitiesAsync(string province, CancellationToken cancellationToken = default)
        => await db.IranianCities
            .AsNoTracking()
            .Include(x => x.Province)
            .Where(x => x.IsActive && x.Province != null && x.Province.IsActive && x.Province.Name == province.Trim())
            .OrderBy(x => x.Name)
            .Select(x => new CityLocationDto(x.Id, x.Name, x.Slug, x.SupportsSameDayDelivery))
            .ToListAsync(cancellationToken);

    private IQueryable<ProvinceLocationDto> QueryLocations()
        => db.IranianProvinces
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new ProvinceLocationDto(
                x.Id,
                x.Name,
                x.Slug,
                x.Cities
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .Select(c => new CityLocationDto(c.Id, c.Name, c.Slug, c.SupportsSameDayDelivery))
                    .ToList()));
}
