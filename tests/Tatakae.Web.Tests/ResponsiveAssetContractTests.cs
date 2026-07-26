namespace Tatakae.Web.Tests;

public sealed class ResponsiveAssetContractTests
{
    private static string Fixture(string name)
        => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));

    [Fact]
    public void Responsive_css_defines_a_dedicated_mobile_contract_for_every_required_page()
    {
        var css = Fixture("phase13-responsive.css");
        var selectors = new[]
        {
            "body[data-page=\"home\"]",
            "[data-page=\"shop\"]",
            "[data-page=\"category\"]",
            "body[data-page=\"product\"]",
            "body[data-page=\"studio\"]",
            "body[data-page=\"checkout\"]",
            "body[data-page=\"login\"]",
            "body[data-page=\"account\"]",
            "body[data-page=\"admin\"]",
            "body[data-page=\"legal\"]",
            ".cart-drawer"
        };

        Assert.All(selectors, selector => Assert.Contains(selector, css));
    }

    [Fact]
    public void Responsive_css_contains_mobile_tablet_compact_touch_and_accessibility_rules()
    {
        var css = Fixture("phase13-responsive.css");

        Assert.Contains("@media (max-width:1180px)", css);
        Assert.Contains("@media (max-width:900px)", css);
        Assert.Contains("@media (max-width:600px)", css);
        Assert.Contains("@media (max-width:420px)", css);
        Assert.Contains("--tap:44px", css);
        Assert.Contains("env(safe-area-inset-bottom", css);
        Assert.Contains("prefers-reduced-motion:reduce", css);
        Assert.Contains(":focus-visible", css);
    }

    [Fact]
    public void Index_loads_phase_13_assets_after_legacy_mobile_rescue()
    {
        var html = Fixture("index.html");
        var rescue = html.IndexOf("css/mobile-rescue.css", StringComparison.Ordinal);
        var responsive = html.IndexOf("css/phase13-responsive.css", StringComparison.Ordinal);

        Assert.True(rescue >= 0);
        Assert.True(responsive > rescue);
        Assert.Contains("js/phase13-responsive.js", html);
    }

    [Fact]
    public void Both_layouts_apply_the_route_marker()
    {
        Assert.Contains("<ResponsiveRouteMarker />", Fixture("MainLayout.razor"));
        Assert.Contains("<ResponsiveRouteMarker />", Fixture("AdminLayout.razor"));
    }

    [Fact]
    public void Shop_and_category_have_mobile_filter_sheet_controls()
    {
        foreach (var file in new[] { "Shop.razor", "Category.razor" })
        {
            var razor = Fixture(file);
            Assert.Contains("mobile-filter-toggle", razor);
            Assert.Contains("mobile-filter-overlay", razor);
            Assert.Contains("mobile-filter-close", razor);
            Assert.Contains("aria-expanded=\"@mobileFiltersOpen\"", razor);
        }
    }

    [Fact]
    public void Route_script_sets_page_and_route_dataset_and_resets_transient_mobile_ui()
    {
        var js = Fixture("phase13-responsive.js");

        Assert.Contains("body.dataset.page", js);
        Assert.Contains("body.dataset.route", js);
        Assert.Contains("mobile-filter-open", js);
        Assert.Contains("orientationchange", js);
    }
}
