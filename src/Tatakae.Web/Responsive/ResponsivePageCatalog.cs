namespace Tatakae.Web.Responsive;

public sealed record ResponsivePageProfile(string Key, string Label, string[] RoutePrefixes);

public static class ResponsivePageCatalog
{
    public static readonly IReadOnlyList<ResponsivePageProfile> Pages =
    [
        new("home", "Home", [""]),
        new("shop", "Shop", ["shop", "products"]),
        new("category", "Category", ["category/", "categories/", "shop/category/"]),
        new("product", "Product", ["product/", "products/"]),
        new("studio", "Studio", ["customize/"]),
        new("checkout", "Checkout", ["checkout", "payment", "order-success"]),
        new("login", "Login", ["login", "register"]),
        new("account", "Account", ["account"]),
        new("admin", "Admin", ["admin"]),
        new("legal", "Legal", ["about", "terms", "rules", "privacy", "returns", "shipping-policy", "contact", "pages/"]),
        new("page", "Generic", [])
    ];

    public static string Resolve(string? relativePath)
    {
        var path = (relativePath ?? string.Empty)
            .Split('?', '#')[0]
            .Trim()
            .Trim('/')
            .ToLowerInvariant();

        if (path.Length == 0) return "home";

        // Specific storefront routes must be evaluated before the broad aliases.
        if (StartsWithAny(path, "shop/category/", "category/", "categories/")) return "category";
        if (StartsWithAny(path, "product/", "products/")) return "product";
        if (path == "shop" || path == "products") return "shop";
        if (path.StartsWith("customize/", StringComparison.Ordinal)) return "studio";
        if (path == "checkout" || path.StartsWith("payment", StringComparison.Ordinal) || path.StartsWith("order-success", StringComparison.Ordinal)) return "checkout";
        if (path == "login" || path == "register") return "login";
        if (path == "account" || path.StartsWith("account/", StringComparison.Ordinal)) return "account";
        if (path == "admin" || path.StartsWith("admin/", StringComparison.Ordinal)) return "admin";
        if (IsLegal(path)) return "legal";
        return "page";
    }

    private static bool IsLegal(string path)
        => path is "about" or "terms" or "rules" or "privacy" or "returns" or "shipping-policy" or "contact"
           || path.StartsWith("pages/", StringComparison.Ordinal);

    private static bool StartsWithAny(string path, params string[] prefixes)
        => prefixes.Any(prefix => path.StartsWith(prefix, StringComparison.Ordinal));
}
