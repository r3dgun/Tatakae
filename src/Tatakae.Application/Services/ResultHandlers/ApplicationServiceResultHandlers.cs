using Microsoft.Extensions.Logging;
using Tatakae.Application.Contracts.Account;
using Tatakae.Application.Contracts.Admin;
using Tatakae.Application.Contracts.Categories;
using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Contracts.Coupons;
using Tatakae.Application.Contracts.Customers;
using Tatakae.Application.Contracts.Embroidery;
using Tatakae.Application.Contracts.Files;
using Tatakae.Application.Contracts.Inventory;
using Tatakae.Application.Contracts.Notifications;
using Tatakae.Application.Contracts.Orders;
using Tatakae.Application.Contracts.Payments;
using Tatakae.Application.Contracts.Products;
using Tatakae.Application.Contracts.Reviews;
using Tatakae.Application.Contracts.Seo;
using Tatakae.Application.Contracts.Shipping;
using Tatakae.Application.Contracts.Wishlist;
using Tatakae.Application.Interfaces;
using Tatakae.Application.Interfaces.Services;
using Tatakae.Domain.Entities;
using Tatakae.Domain.Enums;

namespace Tatakae.Application.Services;

public sealed partial class AccountService
{
    async Task<ResultDto<AccountSessionDto>> IAccountService.RegisterAsync(RegisterCustomerRequest request, CancellationToken cancellationToken)
    {
        var result = new ResultDto<AccountSessionDto>();
        try
        {
            if (request is null) return result.ValidationFailed("اطلاعات ثبت‌نام ارسال نشده است.", "validation_error");
            var data = await RegisterAsync(request, cancellationToken);
            return result.Success("ثبت‌نام با موفقیت انجام شد.", data);
        }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در ثبت‌نام مشتری با موبایل {Mobile}", request?.Mobile);
            return result.Failed("خطایی در ثبت‌نام رخ داده است.");
        }
    }

    async Task<ResultDto<AccountSessionDto>> IAccountService.LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = new ResultDto<AccountSessionDto>();
        try
        {
            if (request is null) return result.ValidationFailed("اطلاعات ورود ارسال نشده است.", "validation_error");
            var data = await LoginAsync(request, cancellationToken);
            return data is null
                ? result.ValidationFailed("شماره موبایل یا اطلاعات ورود معتبر نیست.", "validation_error")
                : result.Success("ورود با موفقیت انجام شد.", data);
        }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در ورود مشتری با موبایل {Mobile}", request?.Mobile);
            return result.Failed("خطایی در ورود رخ داده است.");
        }
    }

    async Task<ResultDto<AccountProfileDto>> IAccountService.ProfileAsync(string mobile, CancellationToken cancellationToken)
    {
        var result = new ResultDto<AccountProfileDto>();
        try
        {
            if (string.IsNullOrWhiteSpace(mobile)) return result.ValidationFailed("شماره موبایل ارسال نشده است.", "validation_error");
            var data = await ProfileAsync(mobile, cancellationToken);
            return data is null ? result.NotFound("حساب کاربری یافت نشد.", "not_found") : result.Success("اطلاعات حساب دریافت شد.", data);
        }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت پروفایل {Mobile}", mobile);
            return result.Failed("خطایی در دریافت اطلاعات حساب رخ داده است.");
        }
    }

    async Task<ResultDto<IReadOnlyCollection<OrderDto>>> IAccountService.OrdersAsync(string mobile, CancellationToken cancellationToken)
    {
        var result = new ResultDto<IReadOnlyCollection<OrderDto>>();
        try
        {
            if (string.IsNullOrWhiteSpace(mobile)) return result.ValidationFailed("شماره موبایل ارسال نشده است.", "validation_error");
            var data = await OrdersAsync(mobile, cancellationToken);
            return result.Success("سفارش‌ها دریافت شدند.", data);
        }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت سفارش‌های {Mobile}", mobile);
            return result.Failed("خطایی در دریافت سفارش‌ها رخ داده است.");
        }
    }

    async Task<ResultDto<OrderTrackingDto>> IAccountService.OrderTrackingAsync(string mobile, Guid orderId, CancellationToken cancellationToken)
    {
        var result = new ResultDto<OrderTrackingDto>();
        try
        {
            if (string.IsNullOrWhiteSpace(mobile) || orderId == Guid.Empty) return result.ValidationFailed("اطلاعات رهگیری سفارش کامل نیست.", "validation_error");
            var data = await OrderTrackingAsync(mobile, orderId, cancellationToken);
            return data is null ? result.NotFound("سفارش موردنظر یافت نشد.", "not_found") : result.Success("اطلاعات رهگیری سفارش دریافت شد.", data);
        }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در رهگیری سفارش {OrderId} برای {Mobile}", orderId, mobile);
            return result.Failed("خطایی در رهگیری سفارش رخ داده است.");
        }
    }

    async Task<ResultDto<IReadOnlyCollection<CustomerAddressDto>>> IAccountService.AddressesAsync(string mobile, CancellationToken cancellationToken)
    {
        var result = new ResultDto<IReadOnlyCollection<CustomerAddressDto>>();
        try
        {
            if (string.IsNullOrWhiteSpace(mobile)) return result.ValidationFailed("شماره موبایل ارسال نشده است.", "validation_error");
            var data = await AddressesAsync(mobile, cancellationToken);
            return result.Success("آدرس‌ها دریافت شدند.", data);
        }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت آدرس‌های {Mobile}", mobile);
            return result.Failed("خطایی در دریافت آدرس‌ها رخ داده است.");
        }
    }

    async Task<ResultDto<CustomerAddressDto>> IAccountService.UpsertAddressAsync(string mobile, Guid? addressId, CustomerAddressRequest request, CancellationToken cancellationToken)
    {
        var result = new ResultDto<CustomerAddressDto>();
        try
        {
            if (string.IsNullOrWhiteSpace(mobile) || request is null) return result.ValidationFailed("اطلاعات آدرس کامل نیست.", "validation_error");
            var data = await UpsertAddressAsync(mobile, addressId, request, cancellationToken);
            return data is null ? result.NotFound("حساب کاربری برای ثبت آدرس یافت نشد.", "not_found") : result.Success("آدرس با موفقیت ذخیره شد.", data);
        }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در ذخیره آدرس {AddressId} برای {Mobile}", addressId, mobile);
            return result.Failed("خطایی در ذخیره آدرس رخ داده است.");
        }
    }

    async Task<ResultDto> IAccountService.DeleteAddressAsync(string mobile, Guid addressId, CancellationToken cancellationToken)
    {
        var result = new ResultDto();
        try
        {
            if (string.IsNullOrWhiteSpace(mobile) || addressId == Guid.Empty) return result.ValidationFailed("شناسه آدرس معتبر نیست.", "validation_error");
            var deleted = await DeleteAddressAsync(mobile, addressId, cancellationToken);
            return deleted ? result.Success("آدرس با موفقیت حذف شد.") : result.NotFound("آدرس موردنظر یافت نشد.", "not_found");
        }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در حذف آدرس {AddressId} برای {Mobile}", addressId, mobile);
            return result.Failed("خطایی در حذف آدرس رخ داده است.");
        }
    }
}

public sealed partial class AdminCatalogService
{
    async Task<ResultDto<IReadOnlyCollection<ProductDetailDto>>> IAdminCatalogService.GetAllAsync(CancellationToken cancellationToken)
    {
        var result = new ResultDto<IReadOnlyCollection<ProductDetailDto>>();
        try { return result.Success("محصولات دریافت شدند.", await GetAllAsync(cancellationToken)); }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در دریافت محصولات مدیریت"); return result.Failed("خطایی در دریافت محصولات رخ داده است."); }
    }

    async Task<ResultDto<ProductDetailDto>> IAdminCatalogService.GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = new ResultDto<ProductDetailDto>();
        try
        {
            if (id == Guid.Empty) return result.ValidationFailed("شناسه محصول معتبر نیست.", "validation_error");
            var data = await GetByIdAsync(id, cancellationToken);
            return data is null ? result.NotFound("محصول یافت نشد.", "not_found") : result.Success("محصول دریافت شد.", data);
        }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در دریافت محصول {ProductId}", id); return result.Failed("خطایی در دریافت محصول رخ داده است."); }
    }

    async Task<ResultDto<ProductDetailDto>> IAdminCatalogService.CreateAsync(AdminProductRequest request, CancellationToken cancellationToken)
    {
        var result = new ResultDto<ProductDetailDto>();
        try
        {
            if (request is null) return result.ValidationFailed("اطلاعات محصول ارسال نشده است.", "validation_error");
            return result.Success("محصول با موفقیت ایجاد شد.", await CreateAsync(request, cancellationToken));
        }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در ایجاد محصول {ProductName}", request?.Name); return result.Failed("خطایی در ایجاد محصول رخ داده است."); }
    }

    async Task<ResultDto<ProductDetailDto>> IAdminCatalogService.UpdateAsync(Guid id, AdminProductRequest request, CancellationToken cancellationToken)
    {
        var result = new ResultDto<ProductDetailDto>();
        try
        {
            if (id == Guid.Empty || request is null) return result.ValidationFailed("اطلاعات محصول برای به‌روزرسانی کامل نیست.", "validation_error");
            return result.Success("محصول با موفقیت به‌روزرسانی شد.", await UpdateAsync(id, request, cancellationToken));
        }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در به‌روزرسانی محصول {ProductId}", id); return result.Failed("خطایی در به‌روزرسانی محصول رخ داده است."); }
    }

    async Task<ResultDto> IAdminCatalogService.DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = new ResultDto();
        try
        {
            if (id == Guid.Empty) return result.ValidationFailed("شناسه محصول معتبر نیست.", "validation_error");
            await DeleteAsync(id, cancellationToken);
            return result.Success("محصول با موفقیت حذف شد.");
        }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در حذف محصول {ProductId}", id); return result.Failed("خطایی در حذف محصول رخ داده است."); }
    }
}

public sealed partial class AdminCategoryService
{
    async Task<ResultDto<IReadOnlyCollection<CategoryDto>>> IAdminCategoryService.GetAllAsync(CancellationToken cancellationToken)
    {
        var result = new ResultDto<IReadOnlyCollection<CategoryDto>>();
        try { return result.Success("دسته‌بندی‌ها دریافت شدند.", await GetAllAsync(cancellationToken)); }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در دریافت دسته‌بندی‌ها"); return result.Failed("خطایی در دریافت دسته‌بندی‌ها رخ داده است."); }
    }

    async Task<ResultDto<CategoryDto>> IAdminCategoryService.CreateAsync(AdminCategoryRequest request, CancellationToken cancellationToken)
    {
        var result = new ResultDto<CategoryDto>();
        try
        {
            if (request is null) return result.ValidationFailed("اطلاعات دسته‌بندی ارسال نشده است.", "validation_error");
            return result.Success("دسته‌بندی با موفقیت ایجاد شد.", await CreateAsync(request, cancellationToken));
        }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در ایجاد دسته‌بندی {Name}", request?.Name); return result.Failed("خطایی در ایجاد دسته‌بندی رخ داده است."); }
    }

    async Task<ResultDto<CategoryDto>> IAdminCategoryService.UpdateAsync(Guid id, AdminCategoryRequest request, CancellationToken cancellationToken)
    {
        var result = new ResultDto<CategoryDto>();
        try
        {
            if (id == Guid.Empty || request is null) return result.ValidationFailed("اطلاعات دسته‌بندی برای به‌روزرسانی کامل نیست.", "validation_error");
            return result.Success("دسته‌بندی با موفقیت به‌روزرسانی شد.", await UpdateAsync(id, request, cancellationToken));
        }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در به‌روزرسانی دسته‌بندی {CategoryId}", id); return result.Failed("خطایی در به‌روزرسانی دسته‌بندی رخ داده است."); }
    }

    async Task<ResultDto> IAdminCategoryService.DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = new ResultDto();
        try { if (id == Guid.Empty) return result.ValidationFailed("شناسه دسته‌بندی معتبر نیست.", "validation_error"); await DeleteAsync(id, cancellationToken); return result.Success("دسته‌بندی با موفقیت حذف شد."); }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در حذف دسته‌بندی {CategoryId}", id); return result.Failed("خطایی در حذف دسته‌بندی رخ داده است."); }
    }
}

public sealed partial class AdminDashboardService
{
    async Task<ResultDto<AdminDashboardDto>> IAdminDashboardService.GetAsync(CancellationToken cancellationToken)
    {
        var result = new ResultDto<AdminDashboardDto>();
        try { return result.Success("اطلاعات داشبورد دریافت شد.", await GetAsync(cancellationToken)); }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در دریافت داشبورد مدیریت"); return result.Failed("خطایی در دریافت داشبورد رخ داده است."); }
    }
}

public sealed partial class CatalogService
{
    async Task<ResultDto<PagedResult<ProductCardDto>>> ICatalogService.GetProductsAsync(ProductListQuery query, CancellationToken cancellationToken)
    {
        var result = new ResultDto<PagedResult<ProductCardDto>>();
        try { if (query is null) return result.ValidationFailed("پارامترهای فهرست محصولات ارسال نشده است.", "validation_error"); return result.Success("محصولات دریافت شدند.", await GetProductsAsync(query, cancellationToken)); }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در دریافت فهرست محصولات"); return result.Failed("خطایی در دریافت محصولات رخ داده است."); }
    }

    async Task<ResultDto<ProductFilterDto>> ICatalogService.GetFiltersAsync(ProductListQuery query, CancellationToken cancellationToken)
    {
        var result = new ResultDto<ProductFilterDto>();
        try { if (query is null) return result.ValidationFailed("پارامترهای فیلتر ارسال نشده است.", "validation_error"); return result.Success("فیلترها دریافت شدند.", await GetFiltersAsync(query, cancellationToken)); }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در دریافت فیلتر محصولات"); return result.Failed("خطایی در دریافت فیلترها رخ داده است."); }
    }

    async Task<ResultDto<ProductDetailDto>> ICatalogService.GetBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        var result = new ResultDto<ProductDetailDto>();
        try
        {
            if (string.IsNullOrWhiteSpace(slug)) return result.ValidationFailed("اسلاگ محصول ارسال نشده است.", "validation_error");
            var data = await GetBySlugAsync(slug, cancellationToken);
            return data is null ? result.NotFound("محصول یافت نشد.", "not_found") : result.Success("محصول دریافت شد.", data);
        }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در دریافت محصول با اسلاگ {Slug}", slug); return result.Failed("خطایی در دریافت محصول رخ داده است."); }
    }

    async Task<ResultDto<IReadOnlyCollection<CategoryDto>>> ICatalogService.GetNavigationCategoriesAsync(CancellationToken cancellationToken)
    {
        var result = new ResultDto<IReadOnlyCollection<CategoryDto>>();
        try { return result.Success("دسته‌بندی‌های منو دریافت شدند.", await GetNavigationCategoriesAsync(cancellationToken)); }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در دریافت دسته‌بندی‌های منو"); return result.Failed("خطایی در دریافت دسته‌بندی‌ها رخ داده است."); }
    }
}

public sealed partial class CustomerService
{
    async Task<ResultDto<IReadOnlyCollection<CustomerDto>>> ICustomerService.GetAllAsync(CancellationToken cancellationToken)
    {
        var result = new ResultDto<IReadOnlyCollection<CustomerDto>>();
        try { return result.Success("مشتریان دریافت شدند.", await GetAllAsync(cancellationToken)); }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در دریافت مشتریان"); return result.Failed("خطایی در دریافت مشتریان رخ داده است."); }
    }
}

public sealed partial class EmbroideryArtworkService
{
    async Task<ResultDto<EmbroideryArtworkDto>> IEmbroideryArtworkService.SubmitAsync(string? mobile, SubmitEmbroideryArtworkRequest request, CancellationToken cancellationToken)
    {
        var result = new ResultDto<EmbroideryArtworkDto>();
        try
        {
            if (request is null) return result.ValidationFailed("اطلاعات طرح ارسال نشده است.", "validation_error");
            var data = await SubmitAsync(mobile, request, cancellationToken);
            return data is null ? result.NotFound("مشتری برای ثبت طرح یافت نشد.", "not_found") : result.Success("طرح با موفقیت ثبت شد.", data);
        }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در ثبت طرح گلدوزی برای {Mobile}", mobile); return result.Failed("خطایی در ثبت طرح رخ داده است."); }
    }

    async Task<ResultDto<IReadOnlyCollection<EmbroideryArtworkDto>>> IEmbroideryArtworkService.GetMineAsync(string mobile, CancellationToken cancellationToken)
    {
        var result = new ResultDto<IReadOnlyCollection<EmbroideryArtworkDto>>();
        try { if (string.IsNullOrWhiteSpace(mobile)) return result.ValidationFailed("شماره موبایل ارسال نشده است.", "validation_error"); return result.Success("طرح‌ها دریافت شدند.", await GetMineAsync(mobile, cancellationToken)); }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در دریافت طرح‌های {Mobile}", mobile); return result.Failed("خطایی در دریافت طرح‌ها رخ داده است."); }
    }

    async Task<ResultDto<IReadOnlyCollection<EmbroideryArtworkDto>>> IEmbroideryArtworkService.AdminListAsync(string? status, CancellationToken cancellationToken)
    {
        var result = new ResultDto<IReadOnlyCollection<EmbroideryArtworkDto>>();
        try { return result.Success("طرح‌های مدیریت دریافت شدند.", await AdminListAsync(status, cancellationToken)); }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در دریافت طرح‌های مدیریت با وضعیت {Status}", status); return result.Failed("خطایی در دریافت طرح‌ها رخ داده است."); }
    }

    async Task<ResultDto<EmbroideryArtworkDto>> IEmbroideryArtworkService.AdminModerateAsync(Guid id, AdminArtworkModerationRequest request, CancellationToken cancellationToken)
    {
        var result = new ResultDto<EmbroideryArtworkDto>();
        try { if (id == Guid.Empty || request is null) return result.ValidationFailed("اطلاعات بررسی طرح کامل نیست.", "validation_error"); return result.Success("وضعیت طرح با موفقیت تغییر کرد.", await AdminModerateAsync(id, request, cancellationToken)); }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در بررسی طرح {ArtworkId}", id); return result.Failed("خطایی در بررسی طرح رخ داده است."); }
    }
}

public sealed partial class EmbroideryPricingService
{
    ResultDto<EmbroideryQuoteDto> IEmbroideryPricingService.Quote(Product product, EmbroideryCustomizationRequest request)
    {
        var result = new ResultDto<EmbroideryQuoteDto>();
        try
        {
            if (product is null || request is null) return result.ValidationFailed("اطلاعات قیمت‌گذاری گلدوزی کامل نیست.", "validation_error");
            return result.Success("قیمت گلدوزی محاسبه شد.", Quote(product, request));
        }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در محاسبه قیمت گلدوزی برای محصول {ProductId}", product?.Id); return result.Failed("خطایی در محاسبه قیمت گلدوزی رخ داده است."); }
    }

    ResultDto<EmbroideryConfiguration> IEmbroideryPricingService.CreateConfiguration(Product product, EmbroideryCustomizationRequest request)
    {
        var result = new ResultDto<EmbroideryConfiguration>();
        try
        {
            if (product is null || request is null) return result.ValidationFailed("اطلاعات پیکربندی گلدوزی کامل نیست.", "validation_error");
            return result.Success("پیکربندی گلدوزی ایجاد شد.", CreateConfiguration(product, request));
        }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در ایجاد پیکربندی گلدوزی برای محصول {ProductId}", product?.Id); return result.Failed("خطایی در ایجاد پیکربندی گلدوزی رخ داده است."); }
    }
}

public sealed partial class InventoryService
{
    async Task<ResultDto<IReadOnlyCollection<InventoryVariantDto>>> IInventoryService.GetInventoryAsync(CancellationToken cancellationToken)
    {
        var result = new ResultDto<IReadOnlyCollection<InventoryVariantDto>>();
        try { return result.Success("موجودی دریافت شد.", await GetInventoryAsync(cancellationToken)); }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در دریافت موجودی"); return result.Failed("خطایی در دریافت موجودی رخ داده است."); }
    }

    async Task<ResultDto<InventoryVariantDto>> IInventoryService.AdjustAsync(InventoryAdjustmentRequest request, CancellationToken cancellationToken)
    {
        var result = new ResultDto<InventoryVariantDto>();
        try { if (request is null) return result.ValidationFailed("اطلاعات تغییر موجودی ارسال نشده است.", "validation_error"); return result.Success("موجودی با موفقیت تغییر کرد.", await AdjustAsync(request, cancellationToken)); }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در تغییر موجودی واریانت {VariantId}", request?.VariantId); return result.Failed("خطایی در تغییر موجودی رخ داده است."); }
    }
}

public sealed partial class MediaAssetService
{
    async Task<ResultDto<IReadOnlyCollection<FileUploadDto>>> IMediaAssetService.GetAllAsync(CancellationToken cancellationToken)
    {
        var result = new ResultDto<IReadOnlyCollection<FileUploadDto>>();
        try { return result.Success("فایل‌ها دریافت شدند.", await GetAllAsync(cancellationToken)); }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در دریافت فایل‌ها"); return result.Failed("خطایی در دریافت فایل‌ها رخ داده است."); }
    }

    async Task<ResultDto<FileUploadDto>> IMediaAssetService.AddStoredFileAsync(CreateStoredFileRequest request, CancellationToken cancellationToken)
    {
        var result = new ResultDto<FileUploadDto>();
        try { if (request is null) return result.ValidationFailed("اطلاعات فایل ارسال نشده است.", "validation_error"); return result.Success("فایل با موفقیت ثبت شد.", await AddStoredFileAsync(request, cancellationToken)); }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در ثبت فایل {FileName}", request?.FileName); return result.Failed("خطایی در ثبت فایل رخ داده است."); }
    }

    async Task<ResultDto> IMediaAssetService.DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = new ResultDto();
        try { if (id == Guid.Empty) return result.ValidationFailed("شناسه فایل معتبر نیست.", "validation_error"); await DeleteAsync(id, cancellationToken); return result.Success("فایل با موفقیت حذف شد."); }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در حذف فایل {FileId}", id); return result.Failed("خطایی در حذف فایل رخ داده است."); }
    }
}

public sealed partial class NotificationService
{
    async Task<ResultDto<NotificationSummaryDto>> INotificationService.GetMineAsync(string mobile, CancellationToken cancellationToken)
    {
        var result = new ResultDto<NotificationSummaryDto>();
        try { if (string.IsNullOrWhiteSpace(mobile)) return result.ValidationFailed("شماره موبایل ارسال نشده است.", "validation_error"); return result.Success("اعلان‌ها دریافت شدند.", await GetMineAsync(mobile, cancellationToken)); }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در دریافت اعلان‌های {Mobile}", mobile); return result.Failed("خطایی در دریافت اعلان‌ها رخ داده است."); }
    }

    async Task<ResultDto<int>> INotificationService.CountUnreadAsync(string mobile, CancellationToken cancellationToken)
    {
        var result = new ResultDto<int>();
        try { if (string.IsNullOrWhiteSpace(mobile)) return result.ValidationFailed("شماره موبایل ارسال نشده است.", "validation_error"); return result.Success("تعداد اعلان‌های خوانده‌نشده دریافت شد.", await CountUnreadAsync(mobile, cancellationToken)); }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در شمارش اعلان‌های {Mobile}", mobile); return result.Failed("خطایی در شمارش اعلان‌ها رخ داده است."); }
    }

    async Task<ResultDto<NotificationDto>> INotificationService.MarkReadAsync(string mobile, Guid notificationId, CancellationToken cancellationToken)
    {
        var result = new ResultDto<NotificationDto>();
        try
        {
            if (string.IsNullOrWhiteSpace(mobile) || notificationId == Guid.Empty) return result.ValidationFailed("اطلاعات اعلان کامل نیست.", "validation_error");
            var data = await MarkReadAsync(mobile, notificationId, cancellationToken);
            return data is null ? result.NotFound("اعلان یافت نشد.", "not_found") : result.Success("اعلان خوانده شد.", data);
        }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در خواندن اعلان {NotificationId}", notificationId); return result.Failed("خطایی در تغییر وضعیت اعلان رخ داده است."); }
    }

    async Task<ResultDto> INotificationService.MarkAllReadAsync(string mobile, CancellationToken cancellationToken)
    {
        var result = new ResultDto();
        try { if (string.IsNullOrWhiteSpace(mobile)) return result.ValidationFailed("شماره موبایل ارسال نشده است.", "validation_error"); await MarkAllReadAsync(mobile, cancellationToken); return result.Success("همه اعلان‌ها خوانده شدند."); }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در خواندن همه اعلان‌های {Mobile}", mobile); return result.Failed("خطایی در تغییر وضعیت اعلان‌ها رخ داده است."); }
    }

    async Task<ResultDto<IReadOnlyCollection<NotificationDto>>> INotificationService.AdminListAsync(AdminNotificationFilter filter, CancellationToken cancellationToken)
    {
        var result = new ResultDto<IReadOnlyCollection<NotificationDto>>();
        try { if (filter is null) return result.ValidationFailed("فیلتر اعلان‌ها ارسال نشده است.", "validation_error"); return result.Success("اعلان‌های مدیریت دریافت شدند.", await AdminListAsync(filter, cancellationToken)); }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در دریافت اعلان‌های مدیریت"); return result.Failed("خطایی در دریافت اعلان‌ها رخ داده است."); }
    }

    async Task<ResultDto<NotificationDto>> INotificationService.AdminCreateAsync(CreateNotificationRequest request, CancellationToken cancellationToken)
    {
        var result = new ResultDto<NotificationDto>();
        try { if (request is null) return result.ValidationFailed("اطلاعات اعلان ارسال نشده است.", "validation_error"); return result.Success("اعلان با موفقیت ایجاد شد.", await AdminCreateAsync(request, cancellationToken)); }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در ایجاد اعلان"); return result.Failed("خطایی در ایجاد اعلان رخ داده است."); }
    }

    async Task<ResultDto<NotificationDto>> INotificationService.AdminUpdateStatusAsync(Guid id, UpdateNotificationStatusRequest request, CancellationToken cancellationToken)
    {
        var result = new ResultDto<NotificationDto>();
        try
        {
            if (id == Guid.Empty || request is null) return result.ValidationFailed("اطلاعات تغییر وضعیت اعلان کامل نیست.", "validation_error");
            var data = await AdminUpdateStatusAsync(id, request, cancellationToken);
            return data is null ? result.NotFound("اعلان یافت نشد.", "not_found") : result.Success("وضعیت اعلان تغییر کرد.", data);
        }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در تغییر وضعیت اعلان {NotificationId}", id); return result.Failed("خطایی در تغییر وضعیت اعلان رخ داده است."); }
    }

    async Task<ResultDto<NotificationDto>> INotificationService.QueueOrderCreatedAsync(OrderDto order, CancellationToken cancellationToken)
    {
        var result = new ResultDto<NotificationDto>();
        try { if (order is null) return result.ValidationFailed("اطلاعات سفارش ارسال نشده است.", "validation_error"); return result.Success("اعلان سفارش در صف قرار گرفت.", await QueueOrderCreatedAsync(order, cancellationToken)); }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در صف اعلان سفارش {OrderId}", order?.Id); return result.Failed("خطایی در ایجاد اعلان سفارش رخ داده است."); }
    }

    async Task<ResultDto<NotificationDto>> INotificationService.QueueOrderStatusChangedAsync(OrderDto order, CancellationToken cancellationToken)
    {
        var result = new ResultDto<NotificationDto>();
        try { if (order is null) return result.ValidationFailed("اطلاعات سفارش ارسال نشده است.", "validation_error"); return result.Success("اعلان تغییر وضعیت سفارش در صف قرار گرفت.", await QueueOrderStatusChangedAsync(order, cancellationToken)); }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در صف اعلان تغییر وضعیت سفارش {OrderId}", order?.Id); return result.Failed("خطایی در ایجاد اعلان سفارش رخ داده است."); }
    }

    async Task<ResultDto<NotificationDto>> INotificationService.QueuePaymentResultAsync(PaymentReceiptDto receipt, string customerMobile, CancellationToken cancellationToken)
    {
        var result = new ResultDto<NotificationDto>();
        try { if (receipt is null || string.IsNullOrWhiteSpace(customerMobile)) return result.ValidationFailed("اطلاعات پرداخت یا مشتری کامل نیست.", "validation_error"); return result.Success("اعلان پرداخت در صف قرار گرفت.", await QueuePaymentResultAsync(receipt, customerMobile, cancellationToken)); }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در صف اعلان پرداخت برای {Mobile}", customerMobile); return result.Failed("خطایی در ایجاد اعلان پرداخت رخ داده است."); }
    }
}

public sealed partial class OrderService
{
    async Task<ResultDto<EmbroideryQuoteDto>> IOrderService.QuoteEmbroideryAsync(EmbroideryCustomizationRequest request, CancellationToken cancellationToken)
    {
        var result = new ResultDto<EmbroideryQuoteDto>();
        try { if (request is null) return result.ValidationFailed("اطلاعات گلدوزی ارسال نشده است.", "validation_error"); return result.Success("قیمت گلدوزی محاسبه شد.", await QuoteEmbroideryAsync(request, cancellationToken)); }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در محاسبه قیمت گلدوزی محصول {ProductId}", request?.ProductId); return result.Failed(ex.Message); }
    }

    async Task<ResultDto<OrderDto>> IOrderService.CheckoutAsync(CheckoutRequest request, CancellationToken cancellationToken)
    {
        var result = new ResultDto<OrderDto>();
        try { if (request is null) return result.ValidationFailed("اطلاعات ثبت سفارش ارسال نشده است.", "validation_error"); return result.Success("سفارش با موفقیت ثبت شد.", await CheckoutAsync(request, cancellationToken)); }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در ثبت سفارش برای {Mobile}", request?.Mobile); return result.Failed(ex.Message); }
    }

    async Task<ResultDto<IReadOnlyCollection<OrderDto>>> IOrderService.GetAllAsync(CancellationToken cancellationToken)
    {
        var result = new ResultDto<IReadOnlyCollection<OrderDto>>();
        try { return result.Success("سفارش‌ها دریافت شدند.", await GetAllAsync(cancellationToken)); }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در دریافت سفارش‌ها"); return result.Failed("خطایی در دریافت سفارش‌ها رخ داده است."); }
    }

    async Task<ResultDto<OrderDto>> IOrderService.GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = new ResultDto<OrderDto>();
        try { if (id == Guid.Empty) return result.ValidationFailed("شناسه سفارش معتبر نیست.", "validation_error"); var data = await GetByIdAsync(id, cancellationToken); return data is null ? result.NotFound("سفارش یافت نشد.", "not_found") : result.Success("سفارش دریافت شد.", data); }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در دریافت سفارش {OrderId}", id); return result.Failed("خطایی در دریافت سفارش رخ داده است."); }
    }

    async Task<ResultDto<OrderDto>> IOrderService.UpdateStatusAsync(Guid id, OrderStatus status, string? trackingCode, string? adminNote, CancellationToken cancellationToken, bool force, string changedBy)
    {
        var result = new ResultDto<OrderDto>();
        try { if (id == Guid.Empty) return result.ValidationFailed("شناسه سفارش معتبر نیست.", "validation_error"); return result.Success("وضعیت سفارش با موفقیت تغییر کرد.", await UpdateStatusAsync(id, status, trackingCode, adminNote, cancellationToken, force, changedBy)); }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در تغییر وضعیت سفارش {OrderId} به {Status}", id, status); return result.Failed(ex.Message); }
    }

    async Task<ResultDto<AdminOrderWorkflowDto>> IOrderService.GetWorkflowAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = new ResultDto<AdminOrderWorkflowDto>();
        try { if (id == Guid.Empty) return result.ValidationFailed("شناسه سفارش معتبر نیست.", "validation_error"); var data = await GetWorkflowAsync(id, cancellationToken); return data is null ? result.NotFound("سفارش یافت نشد.", "not_found") : result.Success("گردش سفارش دریافت شد.", data); }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در دریافت گردش سفارش {OrderId}", id); return result.Failed("خطایی در دریافت گردش سفارش رخ داده است."); }
    }

    ResultDto<IReadOnlyCollection<OrderStatusOptionDto>> IOrderService.GetStatusOptions()
    {
        var result = new ResultDto<IReadOnlyCollection<OrderStatusOptionDto>>();
        try { return result.Success("وضعیت‌های سفارش دریافت شدند.", GetStatusOptions()); }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در دریافت وضعیت‌های سفارش"); return result.Failed("خطایی در دریافت وضعیت‌ها رخ داده است."); }
    }
}

public sealed partial class ProductEngagementService
{
    async Task<ResultDto<IReadOnlyCollection<ProductReviewDto>>> IProductEngagementService.GetApprovedReviewsAsync(Guid productId, CancellationToken cancellationToken)
    {
        var result = new ResultDto<IReadOnlyCollection<ProductReviewDto>>();
        try { if (productId == Guid.Empty) return result.ValidationFailed("شناسه محصول معتبر نیست.", "validation_error"); return result.Success("نظرها دریافت شدند.", await GetApprovedReviewsAsync(productId, cancellationToken)); }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در دریافت نظرهای محصول {ProductId}", productId); return result.Failed("خطایی در دریافت نظرها رخ داده است."); }
    }

    async Task<ResultDto<ProductRatingSummaryDto>> IProductEngagementService.GetRatingSummaryAsync(Guid productId, CancellationToken cancellationToken)
    {
        var result = new ResultDto<ProductRatingSummaryDto>();
        try { if (productId == Guid.Empty) return result.ValidationFailed("شناسه محصول معتبر نیست.", "validation_error"); return result.Success("امتیاز محصول دریافت شد.", await GetRatingSummaryAsync(productId, cancellationToken)); }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در دریافت امتیاز محصول {ProductId}", productId); return result.Failed("خطایی در دریافت امتیاز رخ داده است."); }
    }

    async Task<ResultDto<ProductReviewDto>> IProductEngagementService.CreateReviewAsync(string mobile, CreateProductReviewRequest request, CancellationToken cancellationToken)
    {
        var result = new ResultDto<ProductReviewDto>();
        try { if (string.IsNullOrWhiteSpace(mobile) || request is null) return result.ValidationFailed("اطلاعات نظر کامل نیست.", "validation_error"); var data = await CreateReviewAsync(mobile, request, cancellationToken); return data is null ? result.NotFound("مشتری یا محصول یافت نشد.", "not_found") : result.Success("نظر با موفقیت ثبت شد و در انتظار بررسی است.", data); }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در ثبت نظر محصول {ProductId} توسط {Mobile}", request?.ProductId, mobile); return result.Failed("خطایی در ثبت نظر رخ داده است."); }
    }

    async Task<ResultDto<IReadOnlyCollection<AdminProductReviewDto>>> IProductEngagementService.GetReviewsForAdminAsync(string? status, CancellationToken cancellationToken)
    {
        var result = new ResultDto<IReadOnlyCollection<AdminProductReviewDto>>();
        try { return result.Success("نظرهای مدیریت دریافت شدند.", await GetReviewsForAdminAsync(status, cancellationToken)); }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در دریافت نظرهای مدیریت با وضعیت {Status}", status); return result.Failed("خطایی در دریافت نظرها رخ داده است."); }
    }

    async Task<ResultDto<AdminProductReviewDto>> IProductEngagementService.ModerateReviewAsync(Guid reviewId, AdminReviewModerationRequest request, CancellationToken cancellationToken)
    {
        var result = new ResultDto<AdminProductReviewDto>();
        try { if (reviewId == Guid.Empty || request is null) return result.ValidationFailed("اطلاعات بررسی نظر کامل نیست.", "validation_error"); var data = await ModerateReviewAsync(reviewId, request, cancellationToken); return data is null ? result.NotFound("نظر یافت نشد.", "not_found") : result.Success("وضعیت نظر تغییر کرد.", data); }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در بررسی نظر {ReviewId}", reviewId); return result.Failed("خطایی در بررسی نظر رخ داده است."); }
    }

    async Task<ResultDto<IReadOnlyCollection<ProductQuestionDto>>> IProductEngagementService.GetPublicQuestionsAsync(Guid productId, CancellationToken cancellationToken)
    {
        var result = new ResultDto<IReadOnlyCollection<ProductQuestionDto>>();
        try { if (productId == Guid.Empty) return result.ValidationFailed("شناسه محصول معتبر نیست.", "validation_error"); return result.Success("پرسش‌ها دریافت شدند.", await GetPublicQuestionsAsync(productId, cancellationToken)); }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در دریافت پرسش‌های محصول {ProductId}", productId); return result.Failed("خطایی در دریافت پرسش‌ها رخ داده است."); }
    }

    async Task<ResultDto<ProductQuestionDto>> IProductEngagementService.SubmitQuestionAsync(SubmitProductQuestionRequest request, CancellationToken cancellationToken)
    {
        var result = new ResultDto<ProductQuestionDto>();
        try { if (request is null) return result.ValidationFailed("اطلاعات پرسش ارسال نشده است.", "validation_error"); var data = await SubmitQuestionAsync(request, cancellationToken); return data is null ? result.NotFound("محصول یافت نشد.", "not_found") : result.Success("پرسش با موفقیت ثبت شد و در انتظار بررسی است.", data); }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در ثبت پرسش محصول {ProductId}", request?.ProductId); return result.Failed("خطایی در ثبت پرسش رخ داده است."); }
    }

    async Task<ResultDto<IReadOnlyCollection<AdminProductQuestionDto>>> IProductEngagementService.GetQuestionsForAdminAsync(string? status, CancellationToken cancellationToken)
    {
        var result = new ResultDto<IReadOnlyCollection<AdminProductQuestionDto>>();
        try { return result.Success("پرسش‌های مدیریت دریافت شدند.", await GetQuestionsForAdminAsync(status, cancellationToken)); }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در دریافت پرسش‌های مدیریت با وضعیت {Status}", status); return result.Failed("خطایی در دریافت پرسش‌ها رخ داده است."); }
    }

    async Task<ResultDto<AdminProductQuestionDto>> IProductEngagementService.ModerateQuestionAsync(Guid questionId, AdminQuestionModerationRequest request, Guid? answeredByUserId, CancellationToken cancellationToken)
    {
        var result = new ResultDto<AdminProductQuestionDto>();
        try { if (questionId == Guid.Empty || request is null) return result.ValidationFailed("اطلاعات بررسی پرسش کامل نیست.", "validation_error"); var data = await ModerateQuestionAsync(questionId, request, answeredByUserId, cancellationToken); return data is null ? result.NotFound("پرسش یافت نشد.", "not_found") : result.Success("وضعیت پرسش تغییر کرد.", data); }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در بررسی پرسش {QuestionId}", questionId); return result.Failed("خطایی در بررسی پرسش رخ داده است."); }
    }
}

public sealed partial class SeoService
{
    ResultDto<IReadOnlyCollection<SeoRoutePolicyDto>> ISeoService.GetRoutePolicies()
    {
        var result = new ResultDto<IReadOnlyCollection<SeoRoutePolicyDto>>();
        try { return result.Success("سیاست مسیرهای SEO دریافت شد.", GetRoutePolicies()); }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در دریافت سیاست مسیرهای SEO"); return result.Failed("خطایی در دریافت سیاست‌های SEO رخ داده است."); }
    }

    async Task<ResultDto<SeoSitemapDocumentDto>> ISeoService.GetSitemapAsync(string? publicBaseUrl, CancellationToken cancellationToken)
    {
        var result = new ResultDto<SeoSitemapDocumentDto>();
        try { return result.Success("نقشه سایت تولید شد.", await GetSitemapAsync(publicBaseUrl, cancellationToken)); }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در تولید sitemap برای {BaseUrl}", publicBaseUrl); return result.Failed("خطایی در تولید نقشه سایت رخ داده است."); }
    }

    async Task<ResultDto<SeoAuditSummaryDto>> ISeoService.AuditAsync(string? publicBaseUrl, CancellationToken cancellationToken)
    {
        var result = new ResultDto<SeoAuditSummaryDto>();
        try { return result.Success("ممیزی SEO انجام شد.", await AuditAsync(publicBaseUrl, cancellationToken)); }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در ممیزی SEO برای {BaseUrl}", publicBaseUrl); return result.Failed("خطایی در ممیزی SEO رخ داده است."); }
    }

    async Task<ResultDto<AiSeoDocumentDto>> ISeoService.GetLlmsDocumentAsync(string? publicBaseUrl, AiSeoSiteProfileDto profile, bool includeFullCatalog, CancellationToken cancellationToken)
    {
        var result = new ResultDto<AiSeoDocumentDto>();
        try
        {
            if (profile is null) return result.ValidationFailed("پروفایل SEO هوش مصنوعی ارسال نشده است.", "ai_seo_profile_required");
            var data = await BuildLlmsDocumentAsync(publicBaseUrl, profile, includeFullCatalog, cancellationToken);
            return result.Success("سند قابل‌خواندن برای هوش مصنوعی تولید شد.", data);
        }
        catch (ResultDtoException ex) { return result.Failed(ex); }
        catch (ArgumentException ex) { return result.ValidationFailed(ex.Message, "ai_seo_validation_error"); }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در تولید llms.txt برای {BaseUrl}", publicBaseUrl);
            return result.Failed("خطایی در تولید سند هوش مصنوعی سایت رخ داده است.", ResultStatus.Failure, "ai_seo_document_failed");
        }
    }

    async Task<ResultDto<AiCatalogDocumentDto>> ISeoService.GetAiCatalogAsync(string? publicBaseUrl, AiSeoSiteProfileDto profile, CancellationToken cancellationToken)
    {
        var result = new ResultDto<AiCatalogDocumentDto>();
        try
        {
            if (profile is null) return result.ValidationFailed("پروفایل SEO هوش مصنوعی ارسال نشده است.", "ai_seo_profile_required");
            var data = await BuildAiCatalogAsync(publicBaseUrl, profile, cancellationToken);
            return result.Success("کاتالوگ عمومی هوش مصنوعی تولید شد.", data);
        }
        catch (ResultDtoException ex) { return result.Failed(ex); }
        catch (ArgumentException ex) { return result.ValidationFailed(ex.Message, "ai_seo_validation_error"); }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در تولید کاتالوگ هوش مصنوعی برای {BaseUrl}", publicBaseUrl);
            return result.Failed("خطایی در تولید کاتالوگ هوش مصنوعی رخ داده است.", ResultStatus.Failure, "ai_catalog_failed");
        }
    }
}

public sealed partial class ShippingService
{
    async Task<ResultDto<IReadOnlyCollection<ShippingMethodDto>>> IShippingService.GetAdminMethodsAsync(CancellationToken cancellationToken)
    {
        var result = new ResultDto<IReadOnlyCollection<ShippingMethodDto>>();
        try { return result.Success("روش‌های ارسال دریافت شدند.", await GetAdminMethodsAsync(cancellationToken)); }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در دریافت روش‌های ارسال مدیریت"); return result.Failed("خطایی در دریافت روش‌های ارسال رخ داده است."); }
    }

    async Task<ResultDto<IReadOnlyCollection<ShippingMethodDto>>> IShippingService.GetCheckoutMethodsAsync(ShippingQuoteRequest request, CancellationToken cancellationToken)
    {
        var result = new ResultDto<IReadOnlyCollection<ShippingMethodDto>>();
        try { if (request is null) return result.ValidationFailed("اطلاعات محاسبه ارسال ارسال نشده است.", "validation_error"); return result.Success("روش‌های ارسال قابل انتخاب دریافت شدند.", await GetCheckoutMethodsAsync(request, cancellationToken)); }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در دریافت روش‌های ارسال Checkout"); return result.Failed("خطایی در دریافت روش‌های ارسال رخ داده است."); }
    }

    async Task<ResultDto<ShippingMethodDto>> IShippingService.ResolveCheckoutMethodAsync(string code, decimal cartSubtotal, CancellationToken cancellationToken)
    {
        var result = new ResultDto<ShippingMethodDto>();
        try { if (string.IsNullOrWhiteSpace(code)) return result.ValidationFailed("کد روش ارسال ارسال نشده است.", "validation_error"); return result.Success("روش ارسال محاسبه شد.", await ResolveCheckoutMethodAsync(code, cartSubtotal, cancellationToken)); }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در محاسبه روش ارسال {Code}", code); return result.Failed(ex.Message); }
    }

    async Task<ResultDto<ShippingMethodDto>> IShippingService.UpsertAsync(Guid? id, UpsertManualShippingMethodRequest request, CancellationToken cancellationToken)
    {
        var result = new ResultDto<ShippingMethodDto>();
        try { if (request is null) return result.ValidationFailed("اطلاعات روش ارسال ارسال نشده است.", "validation_error"); return result.Success("روش ارسال با موفقیت ذخیره شد.", await UpsertAsync(id, request, cancellationToken)); }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در ذخیره روش ارسال {ShippingMethodId}", id); return result.Failed("خطایی در ذخیره روش ارسال رخ داده است."); }
    }

    async Task<ResultDto> IShippingService.DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = new ResultDto();
        try { if (id == Guid.Empty) return result.ValidationFailed("شناسه روش ارسال معتبر نیست.", "validation_error"); await DeleteAsync(id, cancellationToken); return result.Success("روش ارسال با موفقیت حذف شد."); }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در حذف روش ارسال {ShippingMethodId}", id); return result.Failed("خطایی در حذف روش ارسال رخ داده است."); }
    }
}

public sealed partial class WishlistService
{
    async Task<ResultDto<WishlistDto>> IWishlistService.GetAsync(string mobile, CancellationToken cancellationToken)
    {
        var result = new ResultDto<WishlistDto>();
        try { if (string.IsNullOrWhiteSpace(mobile)) return result.ValidationFailed("شماره موبایل ارسال نشده است.", "validation_error"); return result.Success("علاقه‌مندی‌ها دریافت شدند.", await GetAsync(mobile, cancellationToken)); }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در دریافت علاقه‌مندی‌های {Mobile}", mobile); return result.Failed("خطایی در دریافت علاقه‌مندی‌ها رخ داده است."); }
    }

    async Task<ResultDto<bool>> IWishlistService.IsWishlistedAsync(string mobile, Guid productId, CancellationToken cancellationToken)
    {
        var result = new ResultDto<bool>();
        try { if (string.IsNullOrWhiteSpace(mobile) || productId == Guid.Empty) return result.ValidationFailed("اطلاعات محصول یا مشتری کامل نیست.", "validation_error"); return result.Success("وضعیت علاقه‌مندی دریافت شد.", await IsWishlistedAsync(mobile, productId, cancellationToken)); }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در بررسی علاقه‌مندی محصول {ProductId} برای {Mobile}", productId, mobile); return result.Failed("خطایی در بررسی علاقه‌مندی رخ داده است."); }
    }

    async Task<ResultDto<WishlistToggleResultDto>> IWishlistService.ToggleAsync(string mobile, Guid productId, CancellationToken cancellationToken)
    {
        var result = new ResultDto<WishlistToggleResultDto>();
        try { if (string.IsNullOrWhiteSpace(mobile) || productId == Guid.Empty) return result.ValidationFailed("اطلاعات محصول یا مشتری کامل نیست.", "validation_error"); var data = await ToggleAsync(mobile, productId, cancellationToken); return data is null ? result.NotFound("مشتری یا محصول یافت نشد.", "not_found") : result.Success("علاقه‌مندی با موفقیت تغییر کرد.", data); }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در تغییر علاقه‌مندی محصول {ProductId} برای {Mobile}", productId, mobile); return result.Failed("خطایی در تغییر علاقه‌مندی رخ داده است."); }
    }

    async Task<ResultDto> IWishlistService.RemoveAsync(string mobile, Guid productId, CancellationToken cancellationToken)
    {
        var result = new ResultDto();
        try { if (string.IsNullOrWhiteSpace(mobile) || productId == Guid.Empty) return result.ValidationFailed("اطلاعات محصول یا مشتری کامل نیست.", "validation_error"); var removed = await RemoveAsync(mobile, productId, cancellationToken); return removed ? result.Success("محصول از علاقه‌مندی‌ها حذف شد.") : result.NotFound("محصول در علاقه‌مندی‌ها یافت نشد.", "not_found"); }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در حذف علاقه‌مندی محصول {ProductId} برای {Mobile}", productId, mobile); return result.Failed("خطایی در حذف علاقه‌مندی رخ داده است."); }
    }

    async Task<ResultDto<IReadOnlyCollection<ProductRecommendationDto>>> IWishlistService.RecommendationsAsync(string mobile, RecommendationQuery query, CancellationToken cancellationToken)
    {
        var result = new ResultDto<IReadOnlyCollection<ProductRecommendationDto>>();
        try { if (string.IsNullOrWhiteSpace(mobile) || query is null) return result.ValidationFailed("اطلاعات پیشنهاد محصول کامل نیست.", "validation_error"); return result.Success("پیشنهادها دریافت شدند.", await RecommendationsAsync(mobile, query, cancellationToken)); }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در پیشنهاد محصول برای {Mobile}", mobile); return result.Failed("خطایی در دریافت پیشنهادها رخ داده است."); }
    }

    async Task<ResultDto<IReadOnlyCollection<ProductRecommendationDto>>> IWishlistService.SimilarAsync(string slug, int take, CancellationToken cancellationToken)
    {
        var result = new ResultDto<IReadOnlyCollection<ProductRecommendationDto>>();
        try { if (string.IsNullOrWhiteSpace(slug)) return result.ValidationFailed("اسلاگ محصول ارسال نشده است.", "validation_error"); return result.Success("محصولات مشابه دریافت شدند.", await SimilarAsync(slug, take, cancellationToken)); }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در دریافت محصولات مشابه {Slug}", slug); return result.Failed("خطایی در دریافت محصولات مشابه رخ داده است."); }
    }
}
