using Microsoft.EntityFrameworkCore;
using Tatakae.Domain.Enums;
using Tatakae.Infrastructure.Persistence;
using Tatakae.Infrastructure.Persistence.Mappers;
using Tatakae.Infrastructure.Persistence.Models;

namespace Tatakae.Infrastructure.Seeding;

/// <summary>
/// Idempotent seed operations that can be executed repeatedly in development and tests.
/// Existing business data is preserved; only missing or structurally broken seed fixtures are repaired.
/// </summary>
public static class StoreDataSeeder
{
    public static Task EnsureCatalogAsync(TatakaeDbContext db, CancellationToken cancellationToken = default)
        => EnsureCatalogAsync(db, includeDevelopmentFixtures: true, cancellationToken: cancellationToken);

    public static async Task EnsureCatalogAsync(
        TatakaeDbContext db,
        bool includeDevelopmentFixtures,
        CancellationToken cancellationToken = default)
    {
        foreach (var category in StoreSeed.CreateCategories().Select(x => x.ToRecord()))
        {
            var existing = await db.Categories
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == category.Id || x.Slug == category.Slug, cancellationToken);

            if (existing is null)
            {
                db.Categories.Add(category);
                continue;
            }

            if (existing.IsRemoved)
            {
                db.Restore(existing);
            }

            existing.Name = category.Name;
            existing.Description = category.Description;
            existing.CoverImageUrl = category.CoverImageUrl;
            existing.SeoMetaTitle = category.SeoMetaTitle;
            existing.SeoMetaDescription = category.SeoMetaDescription;
            existing.SeoCanonicalPath = category.SeoCanonicalPath;
            existing.SeoOpenGraphImageUrl = category.SeoOpenGraphImageUrl;
            existing.SeoAllowIndex = category.SeoAllowIndex;
            existing.SeoAllowFollow = category.SeoAllowFollow;
            existing.SortOrder = category.SortOrder;
            existing.IsActive = true;
        }

        await db.SaveChangesAsync(cancellationToken);

        var products = StoreSeed.CreateProducts()
            .Where(x => includeDevelopmentFixtures || x.Id != DevelopmentSeedCatalog.OutOfStockProductId)
            .Select(x => x.ToRecord());

        foreach (var product in products)
        {
            var existing = await db.Products
                .IgnoreQueryFilters()
                .Include(x => x.Images)
                .Include(x => x.Variants)
                .Include(x => x.Specifications)
                .Include(x => x.Tags)
                .Include(x => x.EmbroideryPolicy)!
                    .ThenInclude(x => x.AllowedPlacements)
                .Include(x => x.EmbroideryPolicy)!
                    .ThenInclude(x => x.AllowedThreadColors)
                .FirstOrDefaultAsync(x => x.Id == product.Id || x.Slug == product.Slug, cancellationToken);

            if (existing is null)
            {
                db.Products.Add(product);
                continue;
            }

            if (existing.IsRemoved)
            {
                db.Restore(existing);
            }

            existing.IsPublished = true;

            EnsureSeedChildren(
                db,
                existing.Images,
                product.Images,
                image => image.ProductId = existing.Id);

            EnsureSeedChildren(
                db,
                existing.Variants,
                product.Variants,
                variant => variant.ProductId = existing.Id);

            EnsureSeedChildren(
                db,
                existing.Specifications,
                product.Specifications,
                specification => specification.ProductId = existing.Id);

            EnsureSeedChildren(
                db,
                existing.Tags,
                product.Tags,
                tag => tag.ProductId = existing.Id);

            if (product.EmbroideryPolicy is not null)
            {
                var seedPolicy = product.EmbroideryPolicy;
                seedPolicy.ProductId = existing.Id;

                if (existing.EmbroideryPolicy is null)
                {
                    db.ProductEmbroideryPolicies.Add(seedPolicy);
                }
                else
                {
                    if (existing.EmbroideryPolicy.IsRemoved)
                    {
                        RestoreSeedEntity(db, existing.EmbroideryPolicy, seedPolicy);
                    }

                    EnsureSeedChildren(
                        db,
                        existing.EmbroideryPolicy.AllowedPlacements,
                        seedPolicy.AllowedPlacements,
                        placement => placement.ProductEmbroideryPolicyId = existing.EmbroideryPolicy.Id);

                    EnsureSeedChildren(
                        db,
                        existing.EmbroideryPolicy.AllowedThreadColors,
                        seedPolicy.AllowedThreadColors,
                        color => color.ProductEmbroideryPolicyId = existing.EmbroideryPolicy.Id);
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public static async Task EnsureDevelopmentFixturesAsync(TatakaeDbContext db, CancellationToken cancellationToken = default)
    {
        await EnsureCustomersAndAddressesAsync(db, cancellationToken);
        await EnsureOrdersAsync(db, cancellationToken);
        await EnsureQuestionsAsync(db, cancellationToken);
    }

    private static async Task EnsureCustomersAndAddressesAsync(TatakaeDbContext db, CancellationToken cancellationToken)
    {
        foreach (var incoming in StoreSeed.CreateCustomers().Select(x => x.ToRecord()))
        {
            var existing = await db.Customers
                .IgnoreQueryFilters()
                .Include(x => x.Addresses)
                .FirstOrDefaultAsync(x => x.Id == incoming.Id || x.Mobile == incoming.Mobile, cancellationToken);

            if (existing is null)
            {
                db.Customers.Add(incoming);
                continue;
            }

            if (existing.IsRemoved)
            {
                db.Restore(existing);
            }

            existing.FullName = incoming.FullName;
            existing.Mobile = incoming.Mobile;
            existing.Email = incoming.Email;

            foreach (var address in incoming.Addresses)
            {
                var storedAddress = existing.Addresses.FirstOrDefault(x => x.Id == address.Id);
                if (storedAddress is null)
                {
                    address.CustomerId = existing.Id;
                    existing.Addresses.Add(address);
                    continue;
                }

                if (storedAddress.IsRemoved)
                {
                    RestoreSeedEntity(db, storedAddress, address);
                }

                storedAddress.RecipientName = address.RecipientName;
                storedAddress.Mobile = address.Mobile;
                storedAddress.Province = address.Province;
                storedAddress.City = address.City;
                storedAddress.PostalCode = address.PostalCode;
                storedAddress.AddressLine = address.AddressLine;
                storedAddress.Plaque = address.Plaque;
                storedAddress.Unit = address.Unit;
                storedAddress.IsDefault = address.IsDefault;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureOrdersAsync(TatakaeDbContext db, CancellationToken cancellationToken)
    {
        foreach (var incoming in StoreSeed.CreateOrders().Select(x => x.ToRecord()))
        {
            var existing = await db.Orders
                .IgnoreQueryFilters()
                .Include(x => x.Lines)
                .Include(x => x.StatusHistory)
                .FirstOrDefaultAsync(x => x.Id == incoming.Id || x.OrderNumber == incoming.OrderNumber, cancellationToken);

            if (existing is null)
            {
                incoming.StatusHistory.Add(CreateSeedOrderHistory(incoming.Id));
                db.Orders.Add(incoming);
                continue;
            }

            if (existing.IsRemoved)
            {
                db.Restore(existing);
            }

            existing.CustomerId = incoming.CustomerId;
            existing.CustomerName = incoming.CustomerName;
            existing.CustomerMobile = incoming.CustomerMobile;
            existing.ShippingRecipientName = incoming.ShippingRecipientName;
            existing.ShippingMobile = incoming.ShippingMobile;
            existing.ShippingProvince = incoming.ShippingProvince;
            existing.ShippingCity = incoming.ShippingCity;
            existing.ShippingPostalCode = incoming.ShippingPostalCode;
            existing.ShippingAddressLine = incoming.ShippingAddressLine;
            existing.ShippingPlaque = incoming.ShippingPlaque;
            existing.ShippingUnit = incoming.ShippingUnit;
            existing.Status = incoming.Status;
            existing.PaymentStatus = incoming.PaymentStatus;
            existing.Subtotal = incoming.Subtotal;
            existing.ShippingAmount = incoming.ShippingAmount;
            existing.ShippingMethodCode = incoming.ShippingMethodCode;
            existing.ShippingMethodTitle = incoming.ShippingMethodTitle;
            existing.DiscountAmount = incoming.DiscountAmount;
            existing.Total = incoming.Total;
            existing.AdminNote = incoming.AdminNote;

            EnsureSeedChildren(
                db,
                existing.Lines,
                incoming.Lines,
                line => line.OrderId = existing.Id);

            var seedHistory = CreateSeedOrderHistory(existing.Id);
            EnsureSeedChildren(
                db,
                existing.StatusHistory,
                [seedHistory],
                history => history.OrderId = existing.Id);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static OrderStatusHistoryDbRecord CreateSeedOrderHistory(Guid orderId) => new()
    {
        Id = SeedIds.From("order-history:test-order:in-embroidery"),
        OrderId = orderId,
        FromStatus = OrderStatus.Paid,
        ToStatus = OrderStatus.InEmbroidery,
        Title = "ورود به مرحله گلدوزی",
        Note = "رکورد تستی فاز ۱۴ برای بررسی timeline سفارش.",
        ChangedBy = "phase14-seed",
        HappenedAt = DevelopmentSeedCatalog.FixedTimestamp.AddDays(1).AddHours(2)
    };

    private static async Task EnsureQuestionsAsync(TatakaeDbContext db, CancellationToken cancellationToken)
    {
        var fixtures = new[]
        {
            new ProductQuestionDbRecord
            {
                Id = DevelopmentSeedCatalog.AnsweredQuestionId,
                ProductId = DevelopmentSeedCatalog.CustomizableProductId,
                CustomerId = DevelopmentSeedCatalog.CustomerId,
                AuthorName = "مشتری تست Tatakae",
                Mobile = DevelopmentSeedCatalog.Customer.Mobile,
                QuestionText = "آیا امکان گلدوزی لوگوی دو رنگ روی سینه چپ این محصول وجود دارد؟",
                AnswerText = "بله؛ این محصول تا شش رنگ نخ و ابعاد حداکثر ۱۲ در ۱۲ سانتی‌متر را پشتیبانی می‌کند.",
                AnsweredByUserId = DevelopmentSeedCatalog.SuperAdmin.UserId,
                Status = QuestionStatus.Answered,
                CreatedAt = DevelopmentSeedCatalog.FixedTimestamp.AddHours(3),
                AnsweredAt = DevelopmentSeedCatalog.FixedTimestamp.AddHours(5),
                ModerationNote = "پرسش‌وپاسخ نمونه برای توسعه و تست صفحه محصول."
            },
            new ProductQuestionDbRecord
            {
                Id = DevelopmentSeedCatalog.PendingQuestionId,
                ProductId = DevelopmentSeedCatalog.ReadyMadeProductId,
                CustomerId = DevelopmentSeedCatalog.CustomerId,
                AuthorName = "مشتری تست Tatakae",
                Mobile = DevelopmentSeedCatalog.Customer.Mobile,
                QuestionText = "آیا این مدل آماده در رنگ دیگری هم دوباره موجود می‌شود؟",
                Status = QuestionStatus.Pending,
                CreatedAt = DevelopmentSeedCatalog.FixedTimestamp.AddDays(2),
                ModerationNote = "پرسش در انتظار پاسخ برای تست پنل مدیریت."
            }
        };

        foreach (var fixture in fixtures)
        {
            var existing = await db.ProductQuestions
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == fixture.Id, cancellationToken);
            if (existing is null)
            {
                db.ProductQuestions.Add(fixture);
                continue;
            }

            if (existing.IsRemoved)
            {
                RestoreSeedEntity(db, existing, fixture);
            }

            existing.ProductId = fixture.ProductId;
            existing.CustomerId = fixture.CustomerId;
            existing.AuthorName = fixture.AuthorName;
            existing.Mobile = fixture.Mobile;
            existing.QuestionText = fixture.QuestionText;
            existing.AnswerText = fixture.AnswerText;
            existing.AnsweredByUserId = fixture.AnsweredByUserId;
            existing.Status = fixture.Status;
            existing.AnsweredAt = fixture.AnsweredAt;
            existing.ModerationNote = fixture.ModerationNote;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static void EnsureSeedChildren<TEntity>(
        TatakaeDbContext db,
        IEnumerable<TEntity> existingEntities,
        IEnumerable<TEntity> seedEntities,
        Action<TEntity> prepareSeedEntity)
        where TEntity : BaseEntity<Guid>
    {
        var existingById = existingEntities.ToDictionary(x => x.Id);

        foreach (var seedEntity in seedEntities)
        {
            prepareSeedEntity(seedEntity);

            if (!existingById.TryGetValue(seedEntity.Id, out var existingEntity))
            {
                db.Set<TEntity>().Add(seedEntity);
                continue;
            }

            if (existingEntity.IsRemoved)
            {
                RestoreSeedEntity(db, existingEntity, seedEntity);
            }
        }
    }

    private static void RestoreSeedEntity<TEntity>(
        TatakaeDbContext db,
        TEntity existingEntity,
        TEntity seedEntity)
        where TEntity : BaseEntity<Guid>
    {
        var insertTime = existingEntity.InsertTime;
        db.Entry(existingEntity).CurrentValues.SetValues(seedEntity);
        existingEntity.InsertTime = insertTime;
        db.Restore(existingEntity);
    }
}
