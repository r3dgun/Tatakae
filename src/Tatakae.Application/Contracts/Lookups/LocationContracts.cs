namespace Tatakae.Application.Contracts.Lookups;

public sealed record CityLocationDto(
    Guid Id,
    string Name,
    string Slug,
    bool SupportsSameDayDelivery);

public sealed record ProvinceLocationDto(
    Guid Id,
    string Name,
    string Slug,
    IReadOnlyCollection<CityLocationDto> Cities);
