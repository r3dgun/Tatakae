using Tatakae.Application.Contracts.Account;
using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Contracts.Notifications;
using Tatakae.Application.Contracts.Orders;
using Tatakae.Web.ApiClients.Abstractions;
using Tatakae.Web.ApiClients.Results;

namespace Tatakae.Web.ApiClients.Http;

public sealed class AccountApiClient(IApiClientTransport transport) : IAccountApiClient
{
    public Task<ResultDto<AccountSessionDto>> RegisterAsync(RegisterCustomerRequest request, CancellationToken cancellationToken = default)
        => transport.SendResultAsync<AccountSessionDto>(HttpMethod.Post, "api/account/register", request, "عضویت ناموفق بود.", cancellationToken);

    public Task<ResultDto<AccountSessionDto>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
        => transport.SendResultAsync<AccountSessionDto>(HttpMethod.Post, "api/account/login", request, "ورود ناموفق بود.", cancellationToken);

    public Task<ResultDto<AccountSessionDto>> MeAsync(CancellationToken cancellationToken = default)
        => transport.GetResultAsync<AccountSessionDto>("api/account/me", "دریافت نشست کاربر ناموفق بود.", cancellationToken);

    public Task<ResultDto> LogoutAsync(CancellationToken cancellationToken = default)
        => transport.SendResultAsync(HttpMethod.Post, "api/account/logout", null, "خروج از حساب ناموفق بود.", cancellationToken);

    public Task<ResultDto<AccountProfileDto>> ProfileAsync(string mobile, CancellationToken cancellationToken = default)
        => transport.GetResultAsync<AccountProfileDto>($"api/account/profile/{Uri.EscapeDataString(mobile)}", "دریافت پروفایل ناموفق بود.", cancellationToken);

    public Task<ResultDto<IReadOnlyCollection<OrderDto>>> OrdersAsync(CancellationToken cancellationToken = default)
        => transport.GetResultAsync<IReadOnlyCollection<OrderDto>>("api/account/orders", "دریافت سفارش‌ها ناموفق بود.", cancellationToken);

    public Task<ResultDto<OrderTrackingDto>> OrderTrackingAsync(Guid orderId, CancellationToken cancellationToken = default)
        => transport.GetResultAsync<OrderTrackingDto>($"api/account/orders/{orderId}/tracking", "دریافت رهگیری سفارش ناموفق بود.", cancellationToken);

    public Task<ResultDto<IReadOnlyCollection<CustomerAddressDto>>> AddressesAsync(CancellationToken cancellationToken = default)
        => transport.GetResultAsync<IReadOnlyCollection<CustomerAddressDto>>("api/account/addresses", "دریافت آدرس‌ها ناموفق بود.", cancellationToken);

    public Task<ResultDto<CustomerAddressDto>> SaveAddressAsync(Guid? id, CustomerAddressRequest model, CancellationToken cancellationToken = default)
        => transport.SendResultAsync<CustomerAddressDto>(
            id.HasValue ? HttpMethod.Put : HttpMethod.Post,
            id.HasValue ? $"api/account/addresses/{id.Value}" : "api/account/addresses",
            model,
            "ذخیره آدرس ناموفق بود.",
            cancellationToken);

    public Task<ResultDto> DeleteAddressAsync(Guid id, CancellationToken cancellationToken = default)
        => transport.SendResultAsync(HttpMethod.Delete, $"api/account/addresses/{id}", null, "حذف آدرس ناموفق بود.", cancellationToken);

    public Task<ResultDto<NotificationSummaryDto>> NotificationsAsync(CancellationToken cancellationToken = default)
        => transport.GetResultAsync<NotificationSummaryDto>("api/account/notifications", "دریافت اعلان‌ها ناموفق بود.", cancellationToken);

    public Task<ResultDto> MarkNotificationReadAsync(Guid id, CancellationToken cancellationToken = default)
        => transport.SendResultAsync(HttpMethod.Patch, $"api/account/notifications/{id}/read", null, "به‌روزرسانی اعلان ناموفق بود.", cancellationToken);

    public Task<ResultDto> MarkAllNotificationsReadAsync(CancellationToken cancellationToken = default)
        => transport.SendResultAsync(HttpMethod.Patch, "api/account/notifications/read-all", null, "به‌روزرسانی اعلان‌ها ناموفق بود.", cancellationToken);
}
