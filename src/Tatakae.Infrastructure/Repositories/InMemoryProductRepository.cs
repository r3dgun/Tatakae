using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Interfaces;
using Tatakae.Domain.Entities;
using Tatakae.Infrastructure.Seeding;

namespace Tatakae.Infrastructure.Repositories;

public sealed class InMemoryProductRepository(ILogger<InMemoryProductRepository>? logger = null) : IProductRepository
{
    private readonly ConcurrentDictionary<Guid, Product> _data = new(StoreSeed.CreateProducts().ToDictionary(x => x.Id));
    private readonly ILogger<InMemoryProductRepository> _logger = logger ?? NullLogger<InMemoryProductRepository>.Instance;

    public Task<ResultDto<IReadOnlyCollection<Product>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var result = new ResultDto<IReadOnlyCollection<Product>>();
        try { return Task.FromResult(result.Success("محصولات دریافت شدند.", _data.Values.ToArray())); }
        catch (Exception ex) { _logger.LogError(ex, "خطا در دریافت محصولات از حافظه"); return Task.FromResult(result.Failed("خطایی در دریافت محصولات رخ داده است.")); }
    }

    public Task<ResultDto<Product>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var result = new ResultDto<Product>();
        if (id == Guid.Empty) return Task.FromResult(result.ValidationFailed("شناسه محصول معتبر نیست."));
        try { return Task.FromResult(_data.TryGetValue(id, out var item) ? result.Success("محصول دریافت شد.", item) : result.NotFound("محصول پیدا نشد.")); }
        catch (Exception ex) { _logger.LogError(ex, "خطا در دریافت محصول {ProductId}", id); return Task.FromResult(result.Failed("خطایی در دریافت محصول رخ داده است.")); }
    }

    public Task<ResultDto<Product>> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var result = new ResultDto<Product>();
        if (string.IsNullOrWhiteSpace(slug)) return Task.FromResult(result.ValidationFailed("اسلاگ محصول معتبر نیست."));
        try
        {
            var item = _data.Values.SingleOrDefault(x => string.Equals(x.Slug, slug.Trim(), StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(item is null ? result.NotFound("محصول پیدا نشد.") : result.Success("محصول دریافت شد.", item));
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در دریافت محصول با اسلاگ {Slug}", slug); return Task.FromResult(result.Failed("خطایی در دریافت محصول رخ داده است.")); }
    }

    public Task<ResultDto<Product>> UpsertAsync(Product product, CancellationToken cancellationToken = default)
    {
        var result = new ResultDto<Product>();
        if (product is null) return Task.FromResult(result.ValidationFailed("اطلاعات محصول ارسال نشده است."));
        try { _data[product.Id] = product; return Task.FromResult(result.Success("محصول ذخیره شد.", product)); }
        catch (Exception ex) { _logger.LogError(ex, "خطا در ذخیره محصول {ProductId}", product.Id); return Task.FromResult(result.Failed("خطایی در ذخیره محصول رخ داده است.")); }
    }

    public Task<ResultDto> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var result = new ResultDto();
        if (id == Guid.Empty) return Task.FromResult(result.ValidationFailed("شناسه محصول معتبر نیست."));
        try { return Task.FromResult(_data.TryRemove(id, out _) ? result.Success("محصول حذف شد.") : result.NotFound("محصول پیدا نشد.")); }
        catch (Exception ex) { _logger.LogError(ex, "خطا در حذف محصول {ProductId}", id); return Task.FromResult(result.Failed("خطایی در حذف محصول رخ داده است.")); }
    }
}
