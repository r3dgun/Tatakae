using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Interfaces;
using Tatakae.Domain.Entities;
using Tatakae.Infrastructure.Seeding;

namespace Tatakae.Infrastructure.Repositories;

public sealed class InMemoryCategoryRepository(ILogger<InMemoryCategoryRepository>? logger = null) : ICategoryRepository
{
    private readonly ConcurrentDictionary<Guid, Category> _data = new(StoreSeed.CreateCategories().ToDictionary(x => x.Id));
    private readonly ILogger<InMemoryCategoryRepository> _logger = logger ?? NullLogger<InMemoryCategoryRepository>.Instance;

    public Task<ResultDto<IReadOnlyCollection<Category>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var result = new ResultDto<IReadOnlyCollection<Category>>();
        try { return Task.FromResult(result.Success("دسته‌بندی‌ها دریافت شدند.", _data.Values.ToArray())); }
        catch (Exception ex) { _logger.LogError(ex, "خطا در دریافت دسته‌بندی‌ها از حافظه"); return Task.FromResult(result.Failed("خطایی در دریافت دسته‌بندی‌ها رخ داده است.")); }
    }

    public Task<ResultDto<Category>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var result = new ResultDto<Category>();
        if (id == Guid.Empty) return Task.FromResult(result.ValidationFailed("شناسه دسته‌بندی معتبر نیست."));
        try { return Task.FromResult(_data.TryGetValue(id, out var item) ? result.Success("دسته‌بندی دریافت شد.", item) : result.NotFound("دسته‌بندی پیدا نشد.")); }
        catch (Exception ex) { _logger.LogError(ex, "خطا در دریافت دسته‌بندی {CategoryId}", id); return Task.FromResult(result.Failed("خطایی در دریافت دسته‌بندی رخ داده است.")); }
    }

    public Task<ResultDto<Category>> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var result = new ResultDto<Category>();
        if (string.IsNullOrWhiteSpace(slug)) return Task.FromResult(result.ValidationFailed("اسلاگ دسته‌بندی معتبر نیست."));
        try
        {
            var item = _data.Values.SingleOrDefault(x => string.Equals(x.Slug, slug.Trim(), StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(item is null ? result.NotFound("دسته‌بندی پیدا نشد.") : result.Success("دسته‌بندی دریافت شد.", item));
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در دریافت دسته‌بندی با اسلاگ {Slug}", slug); return Task.FromResult(result.Failed("خطایی در دریافت دسته‌بندی رخ داده است.")); }
    }

    public Task<ResultDto<Category>> UpsertAsync(Category category, CancellationToken cancellationToken = default)
    {
        var result = new ResultDto<Category>();
        if (category is null) return Task.FromResult(result.ValidationFailed("اطلاعات دسته‌بندی ارسال نشده است."));
        try { _data[category.Id] = category; return Task.FromResult(result.Success("دسته‌بندی ذخیره شد.", category)); }
        catch (Exception ex) { _logger.LogError(ex, "خطا در ذخیره دسته‌بندی {CategoryId}", category.Id); return Task.FromResult(result.Failed("خطایی در ذخیره دسته‌بندی رخ داده است.")); }
    }

    public Task<ResultDto> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var result = new ResultDto();
        if (id == Guid.Empty) return Task.FromResult(result.ValidationFailed("شناسه دسته‌بندی معتبر نیست."));
        try { return Task.FromResult(_data.TryRemove(id, out _) ? result.Success("دسته‌بندی حذف شد.") : result.NotFound("دسته‌بندی پیدا نشد.")); }
        catch (Exception ex) { _logger.LogError(ex, "خطا در حذف دسته‌بندی {CategoryId}", id); return Task.FromResult(result.Failed("خطایی در حذف دسته‌بندی رخ داده است.")); }
    }
}
