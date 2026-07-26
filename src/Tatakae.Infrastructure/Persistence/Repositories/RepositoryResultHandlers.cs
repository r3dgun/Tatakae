using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Contracts.Embroidery;
using Tatakae.Application.Contracts.Files;
using Tatakae.Application.Contracts.Notifications;
using Tatakae.Application.Contracts.Orders;
using Tatakae.Application.Contracts.Products;
using Tatakae.Application.Contracts.Reviews;
using Tatakae.Application.Contracts.Shipping;
using Tatakae.Application.Interfaces;
using Tatakae.Domain.Entities;
using Tatakae.Domain.Enums;
using Tatakae.Infrastructure.Repositories.Common;

namespace Tatakae.Infrastructure.Persistence.Repositories;

public sealed partial class SqlCategoryRepository
{
    Task<ResultDto<IReadOnlyCollection<Category>>> ICategoryRepository.GetAllAsync(CancellationToken ct) => RepositoryResult.QueryAsync(() => GetAllAsync(ct), _resultLogger, "دسته‌بندی‌ها دریافت شدند.", "خطایی در دریافت دسته‌بندی‌ها رخ داده است.", "دریافت دسته‌بندی‌ها");
    Task<ResultDto<Category>> ICategoryRepository.GetByIdAsync(Guid id, CancellationToken ct) => id == Guid.Empty ? Task.FromResult(new ResultDto<Category>().ValidationFailed("شناسه دسته‌بندی معتبر نیست.")) : RepositoryResult.FindAsync(() => GetByIdAsync(id, ct), _resultLogger, "دسته‌بندی دریافت شد.", "دسته‌بندی پیدا نشد.", "خطایی در دریافت دسته‌بندی رخ داده است.", "دریافت دسته‌بندی");
    Task<ResultDto<Category>> ICategoryRepository.GetBySlugAsync(string slug, CancellationToken ct) => string.IsNullOrWhiteSpace(slug) ? Task.FromResult(new ResultDto<Category>().ValidationFailed("اسلاگ دسته‌بندی معتبر نیست.")) : RepositoryResult.FindAsync(() => GetBySlugAsync(slug, ct), _resultLogger, "دسته‌بندی دریافت شد.", "دسته‌بندی پیدا نشد.", "خطایی در دریافت دسته‌بندی رخ داده است.", "دریافت دسته‌بندی با اسلاگ");
    Task<ResultDto<Category>> ICategoryRepository.UpsertAsync(Category category, CancellationToken ct) => category is null ? Task.FromResult(new ResultDto<Category>().ValidationFailed("اطلاعات دسته‌بندی ارسال نشده است.")) : RepositoryResult.MutationAsync(() => UpsertAsync(category, ct), category, _resultLogger, "دسته‌بندی ذخیره شد.", "خطایی در ذخیره دسته‌بندی رخ داده است.", "ذخیره دسته‌بندی");
    Task<ResultDto> ICategoryRepository.DeleteAsync(Guid id, CancellationToken ct) => id == Guid.Empty ? Task.FromResult(new ResultDto().ValidationFailed("شناسه دسته‌بندی معتبر نیست.")) : RepositoryResult.CommandAsync(() => DeleteAsync(id, ct), _resultLogger, "دسته‌بندی حذف شد.", "خطایی در حذف دسته‌بندی رخ داده است.", "حذف دسته‌بندی");
}


public sealed partial class SqlCouponRepository
{
    Task<ResultDto<IReadOnlyCollection<Coupon>>> ICouponRepository.GetAllAsync(CancellationToken ct)
        => RepositoryResult.QueryAsync(
            () => GetAllAsync(ct),
            _resultLogger,
            "کدهای تخفیف دریافت شدند.",
            "خطایی در دریافت کدهای تخفیف رخ داده است.",
            "دریافت کدهای تخفیف");

    Task<ResultDto<Coupon>> ICouponRepository.GetByCodeAsync(string code, CancellationToken ct)
        => string.IsNullOrWhiteSpace(code)
            ? Task.FromResult(new ResultDto<Coupon>().ValidationFailed(
                "کد تخفیف معتبر نیست.",
                "invalid_coupon_code"))
            : RepositoryResult.FindAsync(
                () => GetByCodeAsync(code, ct),
                _resultLogger,
                "کد تخفیف دریافت شد.",
                "کد تخفیف پیدا نشد.",
                "خطایی در دریافت کد تخفیف رخ داده است.",
                "دریافت کد تخفیف");

    Task<ResultDto<Coupon>> ICouponRepository.GetByIdAsync(Guid id, CancellationToken ct)
        => id == Guid.Empty
            ? Task.FromResult(new ResultDto<Coupon>().ValidationFailed(
                "شناسه کد تخفیف معتبر نیست.",
                "invalid_coupon_id"))
            : RepositoryResult.FindAsync(
                () => GetByIdAsync(id, ct),
                _resultLogger,
                "کد تخفیف دریافت شد.",
                "کد تخفیف پیدا نشد.",
                "خطایی در دریافت کد تخفیف رخ داده است.",
                "دریافت کد تخفیف");

    Task<ResultDto<Coupon>> ICouponRepository.UpsertAsync(Coupon coupon, CancellationToken ct)
        => coupon is null
            ? Task.FromResult(new ResultDto<Coupon>().ValidationFailed(
                "اطلاعات کد تخفیف ارسال نشده است.",
                "coupon_required"))
            : RepositoryResult.MutationAsync(
                () => UpsertAsync(coupon, ct),
                coupon,
                _resultLogger,
                "کد تخفیف ذخیره شد.",
                "خطایی در ذخیره کد تخفیف رخ داده است.",
                "ذخیره کد تخفیف");

    Task<ResultDto> ICouponRepository.DeleteAsync(Guid id, CancellationToken ct)
        => id == Guid.Empty
            ? Task.FromResult(new ResultDto().ValidationFailed(
                "شناسه کد تخفیف معتبر نیست.",
                "invalid_coupon_id"))
            : RepositoryResult.CommandAsync(
                () => DeleteAsync(id, ct),
                _resultLogger,
                "کد تخفیف حذف شد.",
                "خطایی در حذف کد تخفیف رخ داده است.",
                "حذف کد تخفیف");
}

public sealed partial class SqlProductRepository
{
    Task<ResultDto<IReadOnlyCollection<Product>>> IProductRepository.GetAllAsync(CancellationToken ct) => RepositoryResult.QueryAsync(() => GetAllAsync(ct), _resultLogger, "محصولات دریافت شدند.", "خطایی در دریافت محصولات رخ داده است.", "دریافت محصولات");
    Task<ResultDto<Product>> IProductRepository.GetByIdAsync(Guid id, CancellationToken ct) => id == Guid.Empty ? Task.FromResult(new ResultDto<Product>().ValidationFailed("شناسه محصول معتبر نیست.")) : RepositoryResult.FindAsync(() => GetByIdAsync(id, ct), _resultLogger, "محصول دریافت شد.", "محصول پیدا نشد.", "خطایی در دریافت محصول رخ داده است.", "دریافت محصول");
    Task<ResultDto<Product>> IProductRepository.GetBySlugAsync(string slug, CancellationToken ct) => string.IsNullOrWhiteSpace(slug) ? Task.FromResult(new ResultDto<Product>().ValidationFailed("اسلاگ محصول معتبر نیست.")) : RepositoryResult.FindAsync(() => GetBySlugAsync(slug, ct), _resultLogger, "محصول دریافت شد.", "محصول پیدا نشد.", "خطایی در دریافت محصول رخ داده است.", "دریافت محصول با اسلاگ");
    Task<ResultDto<Product>> IProductRepository.UpsertAsync(Product product, CancellationToken ct) => product is null ? Task.FromResult(new ResultDto<Product>().ValidationFailed("اطلاعات محصول ارسال نشده است.")) : RepositoryResult.MutationAsync(() => UpsertAsync(product, ct), product, _resultLogger, "محصول ذخیره شد.", "خطایی در ذخیره محصول رخ داده است.", "ذخیره محصول");
    Task<ResultDto> IProductRepository.DeleteAsync(Guid id, CancellationToken ct) => id == Guid.Empty ? Task.FromResult(new ResultDto().ValidationFailed("شناسه محصول معتبر نیست.")) : RepositoryResult.CommandAsync(() => DeleteAsync(id, ct), _resultLogger, "محصول حذف شد.", "خطایی در حذف محصول رخ داده است.", "حذف محصول");
}

public sealed partial class SqlCustomerRepository
{
    Task<ResultDto<Customer>> ICustomerRepository.GetByMobileAsync(string mobile, CancellationToken ct) => string.IsNullOrWhiteSpace(mobile) ? Task.FromResult(new ResultDto<Customer>().ValidationFailed("شماره موبایل معتبر نیست.")) : RepositoryResult.FindAsync(() => GetByMobileAsync(mobile, ct), _resultLogger, "مشتری دریافت شد.", "مشتری پیدا نشد.", "خطایی در دریافت مشتری رخ داده است.", "دریافت مشتری با موبایل");
    Task<ResultDto<Customer>> ICustomerRepository.GetByIdAsync(Guid id, CancellationToken ct) => id == Guid.Empty ? Task.FromResult(new ResultDto<Customer>().ValidationFailed("شناسه مشتری معتبر نیست.")) : RepositoryResult.FindAsync(() => GetByIdAsync(id, ct), _resultLogger, "مشتری دریافت شد.", "مشتری پیدا نشد.", "خطایی در دریافت مشتری رخ داده است.", "دریافت مشتری");
    Task<ResultDto<IReadOnlyCollection<Customer>>> ICustomerRepository.GetAllAsync(CancellationToken ct) => RepositoryResult.QueryAsync(() => GetAllAsync(ct), _resultLogger, "مشتریان دریافت شدند.", "خطایی در دریافت مشتریان رخ داده است.", "دریافت مشتریان");
    Task<ResultDto<Customer>> ICustomerRepository.UpsertAsync(Customer customer, CancellationToken ct) => customer is null ? Task.FromResult(new ResultDto<Customer>().ValidationFailed("اطلاعات مشتری ارسال نشده است.")) : RepositoryResult.MutationAsync(() => UpsertAsync(customer, ct), customer, _resultLogger, "مشتری ذخیره شد.", "خطایی در ذخیره مشتری رخ داده است.", "ذخیره مشتری");
    Task<ResultDto<IReadOnlyCollection<Address>>> ICustomerRepository.GetAddressesAsync(Guid customerId, CancellationToken ct) => customerId == Guid.Empty ? Task.FromResult(new ResultDto<IReadOnlyCollection<Address>>().ValidationFailed("شناسه مشتری معتبر نیست.")) : RepositoryResult.QueryAsync(() => GetAddressesAsync(customerId, ct), _resultLogger, "آدرس‌ها دریافت شدند.", "خطایی در دریافت آدرس‌ها رخ داده است.", "دریافت آدرس‌های مشتری");
    Task<ResultDto<Address>> ICustomerRepository.GetAddressAsync(Guid customerId, Guid addressId, CancellationToken ct) => customerId == Guid.Empty || addressId == Guid.Empty ? Task.FromResult(new ResultDto<Address>().ValidationFailed("شناسه مشتری یا آدرس معتبر نیست.")) : RepositoryResult.FindAsync(() => GetAddressAsync(customerId, addressId, ct), _resultLogger, "آدرس دریافت شد.", "آدرس پیدا نشد.", "خطایی در دریافت آدرس رخ داده است.", "دریافت آدرس");
    Task<ResultDto<Address>> ICustomerRepository.UpsertAddressAsync(Guid customerId, Address address, CancellationToken ct) => customerId == Guid.Empty || address is null ? Task.FromResult(new ResultDto<Address>().ValidationFailed("اطلاعات آدرس کامل نیست.")) : RepositoryResult.MutationAsync(() => UpsertAddressAsync(customerId, address, ct), _resultLogger, "آدرس ذخیره شد.", "خطایی در ذخیره آدرس رخ داده است.", "ذخیره آدرس");
    Task<ResultDto> ICustomerRepository.DeleteAddressAsync(Guid customerId, Guid addressId, CancellationToken ct) => customerId == Guid.Empty || addressId == Guid.Empty ? Task.FromResult(new ResultDto().ValidationFailed("شناسه مشتری یا آدرس معتبر نیست.")) : RepositoryResult.CommandAsync(() => DeleteAddressAsync(customerId, addressId, ct), _resultLogger, "آدرس حذف شد.", "خطایی در حذف آدرس رخ داده است.", "حذف آدرس");
}

public sealed partial class SqlOrderRepository
{
    Task<ResultDto<IReadOnlyCollection<Order>>> IOrderRepository.GetAllAsync(CancellationToken ct) => RepositoryResult.QueryAsync(() => GetAllAsync(ct), _resultLogger, "سفارش‌ها دریافت شدند.", "خطایی در دریافت سفارش‌ها رخ داده است.", "دریافت سفارش‌ها");
    Task<ResultDto<Order>> IOrderRepository.GetByIdAsync(Guid id, CancellationToken ct) => id == Guid.Empty ? Task.FromResult(new ResultDto<Order>().ValidationFailed("شناسه سفارش معتبر نیست.")) : RepositoryResult.FindAsync(() => GetByIdAsync(id, ct), _resultLogger, "سفارش دریافت شد.", "سفارش پیدا نشد.", "خطایی در دریافت سفارش رخ داده است.", "دریافت سفارش");
    Task<ResultDto<Order>> IOrderRepository.GetByNumberAsync(string number, CancellationToken ct) => string.IsNullOrWhiteSpace(number) ? Task.FromResult(new ResultDto<Order>().ValidationFailed("شماره سفارش معتبر نیست.")) : RepositoryResult.FindAsync(() => GetByNumberAsync(number, ct), _resultLogger, "سفارش دریافت شد.", "سفارش پیدا نشد.", "خطایی در دریافت سفارش رخ داده است.", "دریافت سفارش با شماره");
    Task<ResultDto<Order>> IOrderRepository.AddAsync(Order order, CancellationToken ct) => order is null ? Task.FromResult(new ResultDto<Order>().ValidationFailed("اطلاعات سفارش ارسال نشده است.")) : RepositoryResult.MutationAsync(() => AddAsync(order, ct), order, _resultLogger, "سفارش ثبت شد.", "خطایی در ثبت سفارش رخ داده است.", "ثبت سفارش");
    Task<ResultDto<Order>> IOrderRepository.UpdateAsync(Order order, CancellationToken ct) => order is null ? Task.FromResult(new ResultDto<Order>().ValidationFailed("اطلاعات سفارش ارسال نشده است.")) : RepositoryResult.MutationAsync(() => UpdateAsync(order, ct), order, _resultLogger, "سفارش به‌روزرسانی شد.", "خطایی در به‌روزرسانی سفارش رخ داده است.", "به‌روزرسانی سفارش");
    Task<ResultDto<IReadOnlyCollection<OrderStatusHistoryDto>>> IOrderRepository.GetStatusHistoryAsync(Guid orderId, CancellationToken ct) => orderId == Guid.Empty ? Task.FromResult(new ResultDto<IReadOnlyCollection<OrderStatusHistoryDto>>().ValidationFailed("شناسه سفارش معتبر نیست.")) : RepositoryResult.QueryAsync(() => GetStatusHistoryAsync(orderId, ct), _resultLogger, "تاریخچه سفارش دریافت شد.", "خطایی در دریافت تاریخچه سفارش رخ داده است.", "دریافت تاریخچه سفارش");
    async Task<ResultDto<OrderStatusHistoryDto>> IOrderRepository.AddStatusHistoryAsync(Guid orderId, OrderStatus? fromStatus, OrderStatus toStatus, string title, string? note, string? trackingCode, string changedBy, CancellationToken ct)
    {
        if (orderId == Guid.Empty) return new ResultDto<OrderStatusHistoryDto>().ValidationFailed("شناسه سفارش معتبر نیست.");
        var happenedAt = DateTimeOffset.UtcNow;
        var row = new OrderStatusHistoryDto(Guid.NewGuid(), orderId, fromStatus?.ToString(), fromStatus is null ? null : Tatakae.Application.Services.OrderService.StatusLabel(fromStatus.Value), toStatus.ToString(), Tatakae.Application.Services.OrderService.StatusLabel(toStatus), title, note, trackingCode, changedBy, happenedAt);
        return await RepositoryResult.MutationAsync(() => AddStatusHistoryAsync(orderId, fromStatus, toStatus, title, note, trackingCode, changedBy, ct), row, _resultLogger, "تاریخچه سفارش ثبت شد.", "خطایی در ثبت تاریخچه سفارش رخ داده است.", "ثبت تاریخچه سفارش");
    }
}

public sealed partial class SqlMediaAssetRepository
{
    Task<ResultDto<IReadOnlyCollection<FileUploadDto>>> IMediaAssetRepository.GetAllAsync(CancellationToken ct) => RepositoryResult.QueryAsync(() => GetAllAsync(ct), _resultLogger, "فایل‌ها دریافت شدند.", "خطایی در دریافت فایل‌ها رخ داده است.", "دریافت فایل‌ها");
    Task<ResultDto<FileUploadDto>> IMediaAssetRepository.AddAsync(CreateStoredFileRequest request, CancellationToken ct) => request is null ? Task.FromResult(new ResultDto<FileUploadDto>().ValidationFailed("اطلاعات فایل ارسال نشده است.")) : RepositoryResult.MutationAsync(() => AddAsync(request, ct), _resultLogger, "فایل ذخیره شد.", "خطایی در ذخیره فایل رخ داده است.", "ذخیره فایل");
    Task<ResultDto> IMediaAssetRepository.DeleteAsync(Guid id, CancellationToken ct) => id == Guid.Empty ? Task.FromResult(new ResultDto().ValidationFailed("شناسه فایل معتبر نیست.")) : RepositoryResult.CommandAsync(() => DeleteAsync(id, ct), _resultLogger, "فایل حذف شد.", "خطایی در حذف فایل رخ داده است.", "حذف فایل");
}

public sealed partial class SqlShippingMethodRepository
{
    Task<ResultDto<IReadOnlyCollection<ShippingMethodDto>>> IShippingMethodRepository.GetAllAsync(CancellationToken ct) => RepositoryResult.QueryAsync(() => GetAllAsync(ct), _resultLogger, "روش‌های ارسال دریافت شدند.", "خطایی در دریافت روش‌های ارسال رخ داده است.", "دریافت روش‌های ارسال");
    Task<ResultDto<IReadOnlyCollection<ShippingMethodDto>>> IShippingMethodRepository.GetActiveAsync(CancellationToken ct) => RepositoryResult.QueryAsync(() => GetActiveAsync(ct), _resultLogger, "روش‌های ارسال فعال دریافت شدند.", "خطایی در دریافت روش‌های ارسال رخ داده است.", "دریافت روش‌های ارسال فعال");
    Task<ResultDto<ShippingMethodDto>> IShippingMethodRepository.GetByCodeAsync(string code, CancellationToken ct) => string.IsNullOrWhiteSpace(code) ? Task.FromResult(new ResultDto<ShippingMethodDto>().ValidationFailed("کد روش ارسال معتبر نیست.")) : RepositoryResult.FindAsync(() => GetByCodeAsync(code, ct), _resultLogger, "روش ارسال دریافت شد.", "روش ارسال پیدا نشد.", "خطایی در دریافت روش ارسال رخ داده است.", "دریافت روش ارسال با کد");
    Task<ResultDto<ShippingMethodDto>> IShippingMethodRepository.GetByIdAsync(Guid id, CancellationToken ct) => id == Guid.Empty ? Task.FromResult(new ResultDto<ShippingMethodDto>().ValidationFailed("شناسه روش ارسال معتبر نیست.")) : RepositoryResult.FindAsync(() => GetByIdAsync(id, ct), _resultLogger, "روش ارسال دریافت شد.", "روش ارسال پیدا نشد.", "خطایی در دریافت روش ارسال رخ داده است.", "دریافت روش ارسال");
    Task<ResultDto<ShippingMethodDto>> IShippingMethodRepository.UpsertAsync(Guid? id, UpsertManualShippingMethodRequest request, CancellationToken ct) => request is null ? Task.FromResult(new ResultDto<ShippingMethodDto>().ValidationFailed("اطلاعات روش ارسال ارسال نشده است.")) : RepositoryResult.MutationAsync(() => UpsertAsync(id, request, ct), _resultLogger, "روش ارسال ذخیره شد.", "خطایی در ذخیره روش ارسال رخ داده است.", "ذخیره روش ارسال");
    Task<ResultDto> IShippingMethodRepository.DeleteAsync(Guid id, CancellationToken ct) => id == Guid.Empty ? Task.FromResult(new ResultDto().ValidationFailed("شناسه روش ارسال معتبر نیست.")) : RepositoryResult.CommandAsync(() => DeleteAsync(id, ct), _resultLogger, "روش ارسال حذف شد.", "خطایی در حذف روش ارسال رخ داده است.", "حذف روش ارسال");
}

public sealed partial class SqlWishlistRepository
{
    Task<ResultDto<IReadOnlyCollection<WishlistEntry>>> IWishlistRepository.GetByCustomerAsync(Guid customerId, CancellationToken ct) => customerId == Guid.Empty ? Task.FromResult(new ResultDto<IReadOnlyCollection<WishlistEntry>>().ValidationFailed("شناسه مشتری معتبر نیست.")) : RepositoryResult.QueryAsync(() => GetByCustomerAsync(customerId, ct), _resultLogger, "علاقه‌مندی‌ها دریافت شدند.", "خطایی در دریافت علاقه‌مندی‌ها رخ داده است.", "دریافت علاقه‌مندی‌ها");
    Task<ResultDto<bool>> IWishlistRepository.ExistsAsync(Guid customerId, Guid productId, CancellationToken ct) => customerId == Guid.Empty || productId == Guid.Empty ? Task.FromResult(new ResultDto<bool>().ValidationFailed("شناسه مشتری یا محصول معتبر نیست.")) : RepositoryResult.QueryAsync(() => ExistsAsync(customerId, productId, ct), _resultLogger, "وضعیت علاقه‌مندی دریافت شد.", "خطایی در دریافت وضعیت علاقه‌مندی رخ داده است.", "بررسی علاقه‌مندی");
    async Task<ResultDto<WishlistEntry>> IWishlistRepository.AddAsync(Guid customerId, Guid productId, CancellationToken ct)
    {
        if (customerId == Guid.Empty || productId == Guid.Empty) return new ResultDto<WishlistEntry>().ValidationFailed("شناسه مشتری یا محصول معتبر نیست.");
        var command = await RepositoryResult.CommandAsync(() => AddAsync(customerId, productId, ct), _resultLogger, "محصول به علاقه‌مندی‌ها اضافه شد.", "خطایی در افزودن علاقه‌مندی رخ داده است.", "افزودن علاقه‌مندی");
        if (!command.IsSuccess) return command.ForwardFailure<WishlistEntry>();
        var entry = (await GetByCustomerAsync(customerId, ct)).FirstOrDefault(x => x.ProductId == productId);
        return entry is null ? new ResultDto<WishlistEntry>().Failed("علاقه‌مندی ذخیره شد اما بازیابی آن ممکن نبود.") : new ResultDto<WishlistEntry>().Success(command.Message, entry);
    }
    Task<ResultDto> IWishlistRepository.RemoveAsync(Guid customerId, Guid productId, CancellationToken ct) => customerId == Guid.Empty || productId == Guid.Empty ? Task.FromResult(new ResultDto().ValidationFailed("شناسه مشتری یا محصول معتبر نیست.")) : RepositoryResult.CommandAsync(() => RemoveAsync(customerId, productId, ct), _resultLogger, "علاقه‌مندی حذف شد.", "خطایی در حذف علاقه‌مندی رخ داده است.", "حذف علاقه‌مندی");
}

public sealed partial class SqlEmbroideryArtworkRepository
{
    Task<ResultDto<EmbroideryArtworkDto>> IEmbroideryArtworkRepository.SubmitAsync(Guid? customerId, SubmitEmbroideryArtworkRequest request, CancellationToken ct) => request is null ? Task.FromResult(new ResultDto<EmbroideryArtworkDto>().ValidationFailed("اطلاعات طرح گلدوزی ارسال نشده است.")) : RepositoryResult.FindAsync(() => SubmitAsync(customerId, request, ct), _resultLogger, "طرح گلدوزی ثبت شد.", "ثبت طرح گلدوزی انجام نشد.", "خطایی در ثبت طرح گلدوزی رخ داده است.", "ثبت طرح گلدوزی");
    Task<ResultDto<IReadOnlyCollection<EmbroideryArtworkDto>>> IEmbroideryArtworkRepository.GetForCustomerAsync(Guid customerId, CancellationToken ct) => customerId == Guid.Empty ? Task.FromResult(new ResultDto<IReadOnlyCollection<EmbroideryArtworkDto>>().ValidationFailed("شناسه مشتری معتبر نیست.")) : RepositoryResult.QueryAsync(() => GetForCustomerAsync(customerId, ct), _resultLogger, "طرح‌های مشتری دریافت شدند.", "خطایی در دریافت طرح‌ها رخ داده است.", "دریافت طرح‌های مشتری");
    Task<ResultDto<IReadOnlyCollection<EmbroideryArtworkDto>>> IEmbroideryArtworkRepository.GetForAdminAsync(string? status, CancellationToken ct) => RepositoryResult.QueryAsync(() => GetForAdminAsync(status, ct), _resultLogger, "طرح‌ها دریافت شدند.", "خطایی در دریافت طرح‌ها رخ داده است.", "دریافت طرح‌های مدیریت");
    Task<ResultDto<EmbroideryArtworkDto>> IEmbroideryArtworkRepository.GetByIdAsync(Guid id, CancellationToken ct) => id == Guid.Empty ? Task.FromResult(new ResultDto<EmbroideryArtworkDto>().ValidationFailed("شناسه طرح معتبر نیست.")) : RepositoryResult.FindAsync(() => GetByIdAsync(id, ct), _resultLogger, "طرح دریافت شد.", "طرح پیدا نشد.", "خطایی در دریافت طرح رخ داده است.", "دریافت طرح");
    Task<ResultDto<EmbroideryArtworkDto>> IEmbroideryArtworkRepository.ModerateAsync(Guid id, EmbroideryArtworkStatus status, string? adminNote, string? rejectionReason, string? previewImageUrl, string? productionFileExtension, CancellationToken ct) => id == Guid.Empty ? Task.FromResult(new ResultDto<EmbroideryArtworkDto>().ValidationFailed("شناسه طرح معتبر نیست.")) : RepositoryResult.FindAsync(() => ModerateAsync(id, status, adminNote, rejectionReason, previewImageUrl, productionFileExtension, ct), _resultLogger, "وضعیت طرح به‌روزرسانی شد.", "طرح پیدا نشد.", "خطایی در بررسی طرح رخ داده است.", "بررسی طرح");
}

public sealed partial class SqlNotificationRepository
{
    Task<ResultDto<NotificationDto>> INotificationRepository.CreateAsync(CreateNotificationRequest request, CancellationToken ct) => request is null ? Task.FromResult(new ResultDto<NotificationDto>().ValidationFailed("اطلاعات اعلان ارسال نشده است.")) : RepositoryResult.MutationAsync(() => CreateAsync(request, ct), _resultLogger, "اعلان ایجاد شد.", "خطایی در ایجاد اعلان رخ داده است.", "ایجاد اعلان");
    Task<ResultDto<IReadOnlyCollection<NotificationDto>>> INotificationRepository.GetForCustomerAsync(Guid customerId, CancellationToken ct) => customerId == Guid.Empty ? Task.FromResult(new ResultDto<IReadOnlyCollection<NotificationDto>>().ValidationFailed("شناسه مشتری معتبر نیست.")) : RepositoryResult.QueryAsync(() => GetForCustomerAsync(customerId, ct), _resultLogger, "اعلان‌ها دریافت شدند.", "خطایی در دریافت اعلان‌ها رخ داده است.", "دریافت اعلان‌های مشتری");
    Task<ResultDto<IReadOnlyCollection<NotificationDto>>> INotificationRepository.GetForAdminAsync(AdminNotificationFilter filter, CancellationToken ct) => filter is null ? Task.FromResult(new ResultDto<IReadOnlyCollection<NotificationDto>>().ValidationFailed("فیلتر اعلان‌ها ارسال نشده است.")) : RepositoryResult.QueryAsync(() => GetForAdminAsync(filter, ct), _resultLogger, "اعلان‌ها دریافت شدند.", "خطایی در دریافت اعلان‌ها رخ داده است.", "دریافت اعلان‌های مدیریت");
    Task<ResultDto<int>> INotificationRepository.CountUnreadAsync(Guid customerId, CancellationToken ct) => customerId == Guid.Empty ? Task.FromResult(new ResultDto<int>().ValidationFailed("شناسه مشتری معتبر نیست.")) : RepositoryResult.QueryAsync(() => CountUnreadAsync(customerId, ct), _resultLogger, "تعداد اعلان‌های خوانده‌نشده دریافت شد.", "خطایی در شمارش اعلان‌ها رخ داده است.", "شمارش اعلان‌ها");
    Task<ResultDto<NotificationDto>> INotificationRepository.MarkReadAsync(Guid customerId, Guid notificationId, CancellationToken ct) => customerId == Guid.Empty || notificationId == Guid.Empty ? Task.FromResult(new ResultDto<NotificationDto>().ValidationFailed("شناسه مشتری یا اعلان معتبر نیست.")) : RepositoryResult.FindAsync(() => MarkReadAsync(customerId, notificationId, ct), _resultLogger, "اعلان خوانده شد.", "اعلان پیدا نشد.", "خطایی در ثبت خواندن اعلان رخ داده است.", "خوانده‌شدن اعلان");
    Task<ResultDto> INotificationRepository.MarkAllReadAsync(Guid customerId, CancellationToken ct) => customerId == Guid.Empty ? Task.FromResult(new ResultDto().ValidationFailed("شناسه مشتری معتبر نیست.")) : RepositoryResult.CommandAsync(() => MarkAllReadAsync(customerId, ct), _resultLogger, "همه اعلان‌ها خوانده شدند.", "خطایی در به‌روزرسانی اعلان‌ها رخ داده است.", "خواندن همه اعلان‌ها");
    Task<ResultDto<NotificationDto>> INotificationRepository.UpdateStatusAsync(Guid id, NotificationStatus status, string? failureReason, CancellationToken ct) => id == Guid.Empty ? Task.FromResult(new ResultDto<NotificationDto>().ValidationFailed("شناسه اعلان معتبر نیست.")) : RepositoryResult.FindAsync(() => UpdateStatusAsync(id, status, failureReason, ct), _resultLogger, "وضعیت اعلان به‌روزرسانی شد.", "اعلان پیدا نشد.", "خطایی در به‌روزرسانی اعلان رخ داده است.", "به‌روزرسانی اعلان");
}

public sealed partial class SqlProductEngagementRepository
{
    Task<ResultDto<IReadOnlyCollection<ProductReviewDto>>> IProductEngagementRepository.GetApprovedReviewsAsync(Guid productId, CancellationToken ct) => productId == Guid.Empty ? Task.FromResult(new ResultDto<IReadOnlyCollection<ProductReviewDto>>().ValidationFailed("شناسه محصول معتبر نیست.")) : RepositoryResult.QueryAsync(() => GetApprovedReviewsAsync(productId, ct), _resultLogger, "نظرها دریافت شدند.", "خطایی در دریافت نظرها رخ داده است.", "دریافت نظرهای محصول");
    Task<ResultDto<ProductRatingSummaryDto>> IProductEngagementRepository.GetRatingSummaryAsync(Guid productId, CancellationToken ct) => productId == Guid.Empty ? Task.FromResult(new ResultDto<ProductRatingSummaryDto>().ValidationFailed("شناسه محصول معتبر نیست.")) : RepositoryResult.QueryAsync(() => GetRatingSummaryAsync(productId, ct), _resultLogger, "خلاصه امتیاز دریافت شد.", "خطایی در دریافت امتیازها رخ داده است.", "دریافت خلاصه امتیاز");
    Task<ResultDto<IReadOnlyCollection<AdminProductReviewDto>>> IProductEngagementRepository.GetReviewsForAdminAsync(string? status, CancellationToken ct) => RepositoryResult.QueryAsync(() => GetReviewsForAdminAsync(status, ct), _resultLogger, "نظرها دریافت شدند.", "خطایی در دریافت نظرها رخ داده است.", "دریافت نظرهای مدیریت");
    Task<ResultDto<ProductReviewDto>> IProductEngagementRepository.AddReviewAsync(ProductReviewSubmission submission, CancellationToken ct) => submission is null ? Task.FromResult(new ResultDto<ProductReviewDto>().ValidationFailed("اطلاعات نظر ارسال نشده است.")) : RepositoryResult.FindAsync(() => AddReviewAsync(submission, ct), _resultLogger, "نظر ثبت شد.", "ثبت نظر انجام نشد.", "خطایی در ثبت نظر رخ داده است.", "ثبت نظر");
    Task<ResultDto<bool>> IProductEngagementRepository.HasCustomerReviewedAsync(Guid customerId, Guid productId, CancellationToken ct) => customerId == Guid.Empty || productId == Guid.Empty ? Task.FromResult(new ResultDto<bool>().ValidationFailed("شناسه مشتری یا محصول معتبر نیست.")) : RepositoryResult.QueryAsync(() => HasCustomerReviewedAsync(customerId, productId, ct), _resultLogger, "وضعیت نظر مشتری دریافت شد.", "خطایی در بررسی نظر مشتری رخ داده است.", "بررسی نظر مشتری");
    Task<ResultDto<AdminProductReviewDto>> IProductEngagementRepository.ModerateReviewAsync(Guid reviewId, ReviewStatus status, string? adminReply, string? moderationNote, CancellationToken ct) => reviewId == Guid.Empty ? Task.FromResult(new ResultDto<AdminProductReviewDto>().ValidationFailed("شناسه نظر معتبر نیست.")) : RepositoryResult.FindAsync(() => ModerateReviewAsync(reviewId, status, adminReply, moderationNote, ct), _resultLogger, "نظر بررسی شد.", "نظر پیدا نشد.", "خطایی در بررسی نظر رخ داده است.", "بررسی نظر");
    Task<ResultDto<IReadOnlyCollection<ProductQuestionDto>>> IProductEngagementRepository.GetPublicQuestionsAsync(Guid productId, CancellationToken ct) => productId == Guid.Empty ? Task.FromResult(new ResultDto<IReadOnlyCollection<ProductQuestionDto>>().ValidationFailed("شناسه محصول معتبر نیست.")) : RepositoryResult.QueryAsync(() => GetPublicQuestionsAsync(productId, ct), _resultLogger, "پرسش‌ها دریافت شدند.", "خطایی در دریافت پرسش‌ها رخ داده است.", "دریافت پرسش‌های محصول");
    Task<ResultDto<IReadOnlyCollection<AdminProductQuestionDto>>> IProductEngagementRepository.GetQuestionsForAdminAsync(string? status, CancellationToken ct) => RepositoryResult.QueryAsync(() => GetQuestionsForAdminAsync(status, ct), _resultLogger, "پرسش‌ها دریافت شدند.", "خطایی در دریافت پرسش‌ها رخ داده است.", "دریافت پرسش‌های مدیریت");
    Task<ResultDto<ProductQuestionDto>> IProductEngagementRepository.AddQuestionAsync(ProductQuestionSubmission submission, CancellationToken ct) => submission is null ? Task.FromResult(new ResultDto<ProductQuestionDto>().ValidationFailed("اطلاعات پرسش ارسال نشده است.")) : RepositoryResult.FindAsync(() => AddQuestionAsync(submission, ct), _resultLogger, "پرسش ثبت شد.", "ثبت پرسش انجام نشد.", "خطایی در ثبت پرسش رخ داده است.", "ثبت پرسش");
    Task<ResultDto<AdminProductQuestionDto>> IProductEngagementRepository.ModerateQuestionAsync(Guid questionId, QuestionStatus status, string? answerText, string? moderationNote, Guid? answeredByUserId, CancellationToken ct) => questionId == Guid.Empty ? Task.FromResult(new ResultDto<AdminProductQuestionDto>().ValidationFailed("شناسه پرسش معتبر نیست.")) : RepositoryResult.FindAsync(() => ModerateQuestionAsync(questionId, status, answerText, moderationNote, answeredByUserId, ct), _resultLogger, "پرسش بررسی شد.", "پرسش پیدا نشد.", "خطایی در بررسی پرسش رخ داده است.", "بررسی پرسش");
}
