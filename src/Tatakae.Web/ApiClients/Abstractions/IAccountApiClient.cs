using Tatakae.Application.Contracts.Common;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Tatakae.Application.Contracts.Account;
using Tatakae.Application.Contracts.Orders;
using Tatakae.Application.Contracts.Notifications;

namespace Tatakae.Web.ApiClients.Abstractions;

public interface IAccountApiClient
{
    Task<ResultDto<AccountSessionDto>> RegisterAsync(RegisterCustomerRequest request, CancellationToken cancellationToken = default);
    Task<ResultDto<AccountSessionDto>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<ResultDto<AccountSessionDto>> MeAsync(CancellationToken cancellationToken = default);
    Task<ResultDto> LogoutAsync(CancellationToken cancellationToken = default);
    Task<ResultDto<AccountProfileDto>> ProfileAsync(string mobile, CancellationToken cancellationToken = default);
    Task<ResultDto<IReadOnlyCollection<OrderDto>>> OrdersAsync(CancellationToken cancellationToken = default);
    Task<ResultDto<OrderTrackingDto>> OrderTrackingAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<ResultDto<IReadOnlyCollection<CustomerAddressDto>>> AddressesAsync(CancellationToken cancellationToken = default);
    Task<ResultDto<CustomerAddressDto>> SaveAddressAsync(Guid? id, CustomerAddressRequest model, CancellationToken cancellationToken = default);
    Task<ResultDto> DeleteAddressAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ResultDto<NotificationSummaryDto>> NotificationsAsync(CancellationToken cancellationToken = default);
    Task<ResultDto> MarkNotificationReadAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ResultDto> MarkAllNotificationsReadAsync(CancellationToken cancellationToken = default);
}
