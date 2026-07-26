using Tatakae.Domain.Entities;

namespace Tatakae.Infrastructure.Seeding;

public sealed record DevelopmentSeedCredential(
    Guid UserId,
    string Mobile,
    string Password,
    string Email,
    string FullName,
    string Role);

/// <summary>
/// Stable identifiers and credentials for local development and automated tests.
/// These fixtures are inserted only when SeedData:IncludeDevelopmentFixtures is enabled.
/// </summary>
public static class DevelopmentSeedCatalog
{
    public static readonly DateTimeOffset FixedTimestamp = new(2026, 1, 15, 9, 30, 0, TimeSpan.Zero);

    public static readonly Guid CustomizableProductId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    public static readonly Guid ReadyMadeProductId = Guid.Parse("10000000-0000-0000-0000-000000000006");
    public static readonly Guid DiscountedProductId = Guid.Parse("10000000-0000-0000-0000-000000000003");
    public static readonly Guid OutOfStockProductId = Guid.Parse("10000000-0000-0000-0000-000000000008");

    public static readonly Guid CustomerId = Guid.Parse("dd111111-1111-1111-1111-111111111111");
    public static readonly Guid CustomerAddressId = Guid.Parse("ad111111-1111-1111-1111-111111111111");
    public static readonly Guid TestOrderId = Guid.Parse("0d111111-1111-1111-1111-111111111111");
    public const string TestOrderNumber = "EMB-TEST-0001";

    public static readonly Guid AnsweredQuestionId = Guid.Parse("0a111111-1111-1111-1111-111111111111");
    public static readonly Guid PendingQuestionId = Guid.Parse("0a222222-2222-2222-2222-222222222222");

    public static DevelopmentSeedCredential SuperAdmin { get; } = new(
        Guid.Parse("99000000-0000-0000-0000-000000000001"),
        "09120000000",
        "Admin@123456",
        "admin@tatakae.local",
        "مدیر کل Tatakae",
        "SuperAdmin");

    public static DevelopmentSeedCredential DemoAdmin { get; } = new(
        Guid.Parse("99000000-0000-0000-0000-000000000002"),
        "09123456789",
        "Admin@123456",
        "demo.admin@tatakae.local",
        "ادمین تست فروشگاه",
        "SuperAdmin");

    public static DevelopmentSeedCredential Customer { get; } = new(
        Guid.Parse("99000000-0000-0000-0000-000000000003"),
        "09121234567",
        "Customer@123456",
        "customer@tatakae.local",
        "مشتری تست Tatakae",
        "Customer");

    public static IReadOnlyCollection<DevelopmentSeedCredential> Credentials =>
        [SuperAdmin, DemoAdmin, Customer];

    public static IReadOnlyCollection<Product> CreateProducts() => StoreSeed.CreateProducts();
    public static IReadOnlyCollection<Customer> CreateCustomers() => StoreSeed.CreateCustomers();
    public static IReadOnlyCollection<Order> CreateOrders() => StoreSeed.CreateOrders();
}
