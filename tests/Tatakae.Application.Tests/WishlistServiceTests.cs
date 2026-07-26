using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Contracts.Wishlist;
using Tatakae.Application.Interfaces;
using Tatakae.Application.Services;
using Tatakae.Domain.Entities;
using Tatakae.Domain.Enums;

namespace Tatakae.Application.Tests;

public sealed class WishlistServiceTests
{
    private static readonly Guid CategoryA = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid CategoryB = Guid.Parse("10000000-0000-0000-0000-000000000002");
    private static readonly Guid CustomerId = Guid.Parse("20000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task ToggleAsync_WhenProductIsNotInWishlist_AddsIt()
    {
        var product = Product("ink-tee", CategoryA, tags: ["minimal"]);
        var wishlist = new FakeWishlistRepository();
        var service = CreateService([product], wishlist);

        var result = await service.ToggleAsync("09120000000", product.Id);

        Assert.NotNull(result);
        Assert.True(result!.IsWishlisted);
        Assert.Equal(1, result.Count);
        Assert.True((await wishlist.ExistsAsync(CustomerId, product.Id)).RequireData());
    }

    [Fact]
    public async Task ToggleAsync_WhenProductAlreadyExists_RemovesIt()
    {
        var product = Product("ink-hoodie", CategoryA, tags: ["hoodie"]);
        var wishlist = new FakeWishlistRepository(new WishlistEntry(Guid.NewGuid(), CustomerId, product.Id, DateTimeOffset.UtcNow));
        var service = CreateService([product], wishlist);

        var result = await service.ToggleAsync("09120000000", product.Id);

        Assert.NotNull(result);
        Assert.False(result!.IsWishlisted);
        Assert.Equal(0, result.Count);
        Assert.False((await wishlist.ExistsAsync(CustomerId, product.Id)).RequireData());
    }

    [Fact]
    public async Task RecommendationsAsync_ExcludesWishlistItemsAndPrioritizesSameCategoryAndTags()
    {
        var liked = Product("liked-tee", CategoryA, tags: ["dragon", "minimal"]);
        var best = Product("best-match", CategoryA, tags: ["dragon", "minimal"], featured: true);
        var weak = Product("weak-match", CategoryB, tags: ["plain"]);
        var wishlist = new FakeWishlistRepository(new WishlistEntry(Guid.NewGuid(), CustomerId, liked.Id, DateTimeOffset.UtcNow));
        var service = CreateService([liked, weak, best], wishlist);

        var recommendations = await service.RecommendationsAsync("09120000000", new RecommendationQuery { Take = 2 });

        Assert.DoesNotContain(recommendations, x => x.Product.Id == liked.Id);
        Assert.Equal(best.Id, recommendations.First().Product.Id);
        Assert.Contains("علاقه", recommendations.First().Reason);
    }

    [Fact]
    public async Task SimilarAsync_ExcludesCurrentProduct()
    {
        var current = Product("current", CategoryA, tags: ["samurai"]);
        var similar = Product("similar", CategoryA, tags: ["samurai"]);
        var service = CreateService([current, similar], new FakeWishlistRepository());

        var items = await service.SimilarAsync("current", 4);

        Assert.Single(items);
        Assert.Equal(similar.Id, items.Single().Product.Id);
    }

    [Fact]
    public void RecommendationEngine_ReturnsNegativeScoreForOutOfStockProducts()
    {
        var product = Product("sold-out", CategoryA, stock: 0);

        var score = ProductRecommendationEngine.Score(product, Array.Empty<Product>());

        Assert.True(score < 0);
    }

    private static WishlistService CreateService(IReadOnlyCollection<Product> products, IWishlistRepository wishlist)
    {
        var categories = new[]
        {
            new Category(CategoryA, "تی‌شرت", "tshirts", "", null, Seo()),
            new Category(CategoryB, "هودی", "hoodies", "", null, Seo())
        };

        return new WishlistService(
            wishlist,
            new FakeCustomerRepository(Customer.Create(CustomerId, "مشتری تست", "09120000000", null, DateTimeOffset.UnixEpoch)),
            new FakeProductRepository(products),
            new FakeCategoryRepository(categories));
    }

    private static Product Product(string slug, Guid categoryId, IReadOnlyCollection<string>? tags = null, bool featured = false, int stock = 8)
        => Tatakae.Domain.Entities.Product.Create(
            Guid.NewGuid(),
            slug.Replace('-', ' '),
            slug,
            ApparelCategory.TShirt,
            categoryId,
            "توضیح کوتاه محصول تستی برای فروشگاه Tatakae",
            "توضیح کامل محصول تستی برای سناریوی پیشنهاد و علاقه‌مندی در فروشگاه Tatakae.",
            "پنبه",
            "Regular",
            "شستشو با آب سرد",
            "",
            Seo(),
            Policy(),
            [new ProductImage(Guid.NewGuid(), "https://example.com/p.jpg", "محصول", true, 0)],
            [new ProductVariant(Guid.NewGuid(), $"TT-{slug.ToUpperInvariant()}", "M", "مشکی", "#111111", 900_000m, null, stock)],
            Array.Empty<ProductSpecification>(),
            tags ?? Array.Empty<string>(),
            isPublished: true,
            isFeatured: featured,
            supportsEmbroidery: true,
            createdAt: DateTimeOffset.UnixEpoch);

    private static SeoMetadata Seo() => new("title", "description", null, null, true, true);

    private static EmbroideryPolicy Policy() => new(0, 0, 0, 8, 20, 20, [EmbroideryPlacement.LeftChest], ["#111111"]);

    private sealed class FakeWishlistRepository(params WishlistEntry[] entries) : IWishlistRepository
    {
        private readonly List<WishlistEntry> _entries = entries.ToList();

        public Task<ResultDto<IReadOnlyCollection<WishlistEntry>>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<IReadOnlyCollection<WishlistEntry>>()
                .Success("علاقه‌مندی‌ها دریافت شدند.", _entries.Where(x => x.CustomerId == customerId).ToArray()));

        public Task<ResultDto<bool>> ExistsAsync(Guid customerId, Guid productId, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<bool>()
                .Success("وضعیت علاقه‌مندی دریافت شد.", _entries.Any(x => x.CustomerId == customerId && x.ProductId == productId)));

        public Task<ResultDto<WishlistEntry>> AddAsync(Guid customerId, Guid productId, CancellationToken cancellationToken = default)
        {
            var existing = _entries.FirstOrDefault(x => x.CustomerId == customerId && x.ProductId == productId);
            if (existing is not null)
                return Task.FromResult(new ResultDto<WishlistEntry>().Success("محصول از قبل در علاقه‌مندی‌ها قرار دارد.", existing));

            var entry = new WishlistEntry(Guid.NewGuid(), customerId, productId, DateTimeOffset.UtcNow);
            _entries.Add(entry);
            return Task.FromResult(new ResultDto<WishlistEntry>().Success("محصول به علاقه‌مندی‌ها اضافه شد.", entry));
        }

        public Task<ResultDto> RemoveAsync(Guid customerId, Guid productId, CancellationToken cancellationToken = default)
        {
            var count = _entries.RemoveAll(x => x.CustomerId == customerId && x.ProductId == productId);
            return Task.FromResult(count > 0
                ? new ResultDto().Success("محصول از علاقه‌مندی‌ها حذف شد.")
                : new ResultDto().NotFound("علاقه‌مندی پیدا نشد."));
        }
    }

    private sealed class FakeCustomerRepository(Customer customer) : ICustomerRepository
    {
        public Task<ResultDto<Customer>> GetByMobileAsync(string mobile, CancellationToken cancellationToken = default)
            => Task.FromResult(mobile == customer.Mobile
                ? new ResultDto<Customer>().Success("مشتری دریافت شد.", customer)
                : new ResultDto<Customer>().NotFound("مشتری پیدا نشد."));

        public Task<ResultDto<Customer>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(id == customer.Id
                ? new ResultDto<Customer>().Success("مشتری دریافت شد.", customer)
                : new ResultDto<Customer>().NotFound("مشتری پیدا نشد."));

        public Task<ResultDto<IReadOnlyCollection<Customer>>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<IReadOnlyCollection<Customer>>().Success("مشتریان دریافت شدند.", [customer]));

        public Task<ResultDto<Customer>> UpsertAsync(Customer item, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<Customer>().Success("مشتری ذخیره شد.", item));

        public Task<ResultDto<IReadOnlyCollection<Address>>> GetAddressesAsync(Guid customerId, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<IReadOnlyCollection<Address>>().Success("آدرس‌ها دریافت شدند.", Array.Empty<Address>()));

        public Task<ResultDto<Address>> GetAddressAsync(Guid customerId, Guid addressId, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<Address>().NotFound("آدرس پیدا نشد."));

        public Task<ResultDto<Address>> UpsertAddressAsync(Guid customerId, Address address, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<Address>().Success("آدرس ذخیره شد.", address));

        public Task<ResultDto> DeleteAddressAsync(Guid customerId, Guid addressId, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto().NotFound("آدرس پیدا نشد."));
    }

    private sealed class FakeProductRepository(IReadOnlyCollection<Product> products) : IProductRepository
    {
        public Task<ResultDto<IReadOnlyCollection<Product>>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<IReadOnlyCollection<Product>>().Success("محصولات دریافت شدند.", products));

        public Task<ResultDto<Product>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var item = products.SingleOrDefault(x => x.Id == id);
            return Task.FromResult(item is null
                ? new ResultDto<Product>().NotFound("محصول پیدا نشد.")
                : new ResultDto<Product>().Success("محصول دریافت شد.", item));
        }

        public Task<ResultDto<Product>> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
        {
            var item = products.SingleOrDefault(x => x.Slug == slug);
            return Task.FromResult(item is null
                ? new ResultDto<Product>().NotFound("محصول پیدا نشد.")
                : new ResultDto<Product>().Success("محصول دریافت شد.", item));
        }

        public Task<ResultDto<Product>> UpsertAsync(Product product, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<Product>().Success("محصول ذخیره شد.", product));

        public Task<ResultDto> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto().Success("محصول حذف شد."));
    }

    private sealed class FakeCategoryRepository(IReadOnlyCollection<Category> categories) : ICategoryRepository
    {
        public Task<ResultDto<IReadOnlyCollection<Category>>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<IReadOnlyCollection<Category>>().Success("دسته‌بندی‌ها دریافت شدند.", categories));

        public Task<ResultDto<Category>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var item = categories.SingleOrDefault(x => x.Id == id);
            return Task.FromResult(item is null
                ? new ResultDto<Category>().NotFound("دسته‌بندی پیدا نشد.")
                : new ResultDto<Category>().Success("دسته‌بندی دریافت شد.", item));
        }

        public Task<ResultDto<Category>> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
        {
            var item = categories.SingleOrDefault(x => x.Slug == slug);
            return Task.FromResult(item is null
                ? new ResultDto<Category>().NotFound("دسته‌بندی پیدا نشد.")
                : new ResultDto<Category>().Success("دسته‌بندی دریافت شد.", item));
        }

        public Task<ResultDto<Category>> UpsertAsync(Category category, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<Category>().Success("دسته‌بندی ذخیره شد.", category));

        public Task<ResultDto> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto().Success("دسته‌بندی حذف شد."));
    }

}
