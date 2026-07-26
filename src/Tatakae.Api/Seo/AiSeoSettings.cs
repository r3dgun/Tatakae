using Microsoft.Extensions.Configuration;
using Tatakae.Application.Contracts.Seo;

namespace Tatakae.Api.Seo;

public sealed class AiSeoSettings
{
    public const string SectionName = "AiSeo";

    public string SiteName { get; set; } = "Tatakae";
    public string OrganizationName { get; set; } = "Tatakae";
    public string Summary { get; set; } = "فروشگاه ایرانی پوشاک گلدوزی آماده و قابل شخصی‌سازی.";
    public string Language { get; set; } = "fa-IR";
    public string Currency { get; set; } = "IRR";
    public string AreaServed { get; set; } = "Iran";
    public string? SupportEmail { get; set; }
    public string? SupportPhone { get; set; }
    public int MaxProductsInLlms { get; set; } = 100;
    public bool ExposeFullCatalog { get; set; } = true;
    public bool AllowOpenAiSearch { get; set; } = true;
    public bool AllowOpenAiUserFetch { get; set; } = true;
    public bool AllowOpenAiTraining { get; set; } = false;

    public AiSeoSiteProfileDto ToProfile()
        => new(
            SiteName,
            OrganizationName,
            Summary,
            Language,
            Currency,
            AreaServed,
            SupportEmail,
            SupportPhone,
            Math.Clamp(MaxProductsInLlms, 1, 500));

    public static AiSeoSettings From(IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);
        var settings = new AiSeoSettings
        {
            SiteName = section[nameof(SiteName)] ?? "Tatakae",
            OrganizationName = section[nameof(OrganizationName)] ?? "Tatakae",
            Summary = section[nameof(Summary)] ?? "فروشگاه ایرانی پوشاک گلدوزی آماده و قابل شخصی‌سازی.",
            Language = section[nameof(Language)] ?? "fa-IR",
            Currency = section[nameof(Currency)] ?? "IRR",
            AreaServed = section[nameof(AreaServed)] ?? "Iran",
            SupportEmail = section[nameof(SupportEmail)],
            SupportPhone = section[nameof(SupportPhone)]
        };

        if (int.TryParse(section[nameof(MaxProductsInLlms)], out var maxProducts)) settings.MaxProductsInLlms = maxProducts;
        if (bool.TryParse(section[nameof(ExposeFullCatalog)], out var exposeFull)) settings.ExposeFullCatalog = exposeFull;
        if (bool.TryParse(section[nameof(AllowOpenAiSearch)], out var allowSearch)) settings.AllowOpenAiSearch = allowSearch;
        if (bool.TryParse(section[nameof(AllowOpenAiUserFetch)], out var allowUserFetch)) settings.AllowOpenAiUserFetch = allowUserFetch;
        if (bool.TryParse(section[nameof(AllowOpenAiTraining)], out var allowTraining)) settings.AllowOpenAiTraining = allowTraining;
        return settings;
    }
}
