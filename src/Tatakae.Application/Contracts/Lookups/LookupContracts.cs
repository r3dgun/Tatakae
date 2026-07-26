namespace Tatakae.Application.Contracts.Lookups;

public sealed record EnumLookupDto(string Value, string Label, int SortOrder);

public sealed record StoreLookupsDto(
    IReadOnlyCollection<EnumLookupDto> ApparelCategories,
    IReadOnlyCollection<EnumLookupDto> EmbroideryPlacements,
    IReadOnlyCollection<EnumLookupDto> OrderStatuses,
    IReadOnlyCollection<EnumLookupDto> PaymentStatuses,
    IReadOnlyCollection<SizeLookupDto> Sizes,
    IReadOnlyCollection<ColorLookupDto> GarmentColors,
    IReadOnlyCollection<ColorLookupDto> ThreadColors);

public sealed record SizeLookupDto(string Value, string Label, int SortOrder);
public sealed record ColorLookupDto(string Name, string Hex, bool IsDark);
