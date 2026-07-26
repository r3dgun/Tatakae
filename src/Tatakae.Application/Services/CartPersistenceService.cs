using Microsoft.Extensions.Logging;
using Tatakae.Application.Contracts.Cart;
using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Interfaces.Gateways;
using Tatakae.Application.Interfaces.Services;

namespace Tatakae.Application.Services;

public sealed class CartPersistenceService(
    ICartPersistenceGateway gateway,
    ILogger<CartPersistenceService> logger) : ICartPersistenceService
{
    public Task<ResultDto<CartMergeResultDto>> MergeAsync(
        MergeCartRequest request,
        CartCustomerContext customer,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
            return Task.FromResult(new ResultDto<CartMergeResultDto>().ValidationFailed("اطلاعات سبد خرید ارسال نشده است.", "cart_merge_request_required"));

        if (customer is null)
            return Task.FromResult(new ResultDto<CartMergeResultDto>().Unauthorized("اطلاعات کاربر معتبر نیست.", "cart_customer_required"));

        return ApplicationServiceResult.ExecuteAsync(
            () => gateway.MergeAsync(request, customer, cancellationToken),
            "سبد خرید با حساب کاربری همگام شد.",
            "خطایی در همگام‌سازی سبد خرید رخ داده است.",
            "cart_merge_failed",
            logger);
    }

    public Task<ResultDto> ClearAsync(CartCustomerContext customer, CancellationToken cancellationToken = default)
        => customer is null
            ? Task.FromResult(new ResultDto().Unauthorized("اطلاعات کاربر معتبر نیست.", "cart_customer_required"))
            : ApplicationServiceResult.ExecuteAsync(
                () => gateway.ClearAsync(customer, cancellationToken),
                "سبد خرید با موفقیت پاک شد.",
                "خطایی در پاک‌کردن سبد خرید رخ داده است.",
                "cart_clear_failed",
                logger);
}
