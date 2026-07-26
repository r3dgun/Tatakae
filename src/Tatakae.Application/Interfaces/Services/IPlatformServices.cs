using Tatakae.Application.Contracts.Account;
using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Contracts.Cart;
using Tatakae.Application.Contracts.Lookups;
using Tatakae.Application.Contracts.Legal;
using Tatakae.Application.Contracts.Payments;
using Tatakae.Application.Contracts.Security;

namespace Tatakae.Application.Interfaces.Services;

/// <summary>
/// Authentication use cases. HTTP-specific values are flattened into transport-neutral
/// request metadata by the API layer before entering the application boundary.
/// </summary>
public interface IIdentityAuthService
{
    Task<ResultDto<AccountSessionDto>> RegisterAsync(
        RegisterCustomerRequest request,
        ClientRequestMetadata metadata,
        CancellationToken cancellationToken = default);

    Task<ResultDto<AccountSessionDto>> LoginAsync(
        LoginRequest request,
        ClientRequestMetadata metadata,
        CancellationToken cancellationToken = default);

    Task<ResultDto<AccountSessionDto>> CurrentAsync(
        AuthenticatedSessionContext session,
        CancellationToken cancellationToken = default);

    Task<ResultDto> LogoutAsync(
        AuthenticatedSessionContext session,
        CancellationToken cancellationToken = default);
}

public interface ILegalContentService
{
    Task<ResultDto<IReadOnlyCollection<StorePolicyPageDto>>> GetPublishedPagesAsync(CancellationToken cancellationToken = default);
    Task<ResultDto<StorePolicyPageDto>> GetPublishedPageAsync(string slug, CancellationToken cancellationToken = default);
    Task<ResultDto<IReadOnlyCollection<StorePolicyPageDto>>> GetAllPagesAsync(CancellationToken cancellationToken = default);
    Task<ResultDto<StorePolicyPageDto>> UpsertPageAsync(string? currentSlug, UpsertStorePolicyPageRequest request, CancellationToken cancellationToken = default);
    Task<ResultDto<ContactMessageDto>> SubmitContactAsync(SubmitContactMessageRequest request, string? ipAddress, CancellationToken cancellationToken = default);
    Task<ResultDto<IReadOnlyCollection<ContactMessageDto>>> GetContactMessagesAsync(CancellationToken cancellationToken = default);
    Task<ResultDto<ContactMessageDto>> UpdateContactMessageAsync(Guid id, UpdateContactMessageStatusRequest request, CancellationToken cancellationToken = default);
}

public interface IPaymentService
{
    Task<ResultDto<PaymentInitDto>> StartAsync(CreatePaymentRequest request, string? currentMobile, CancellationToken cancellationToken = default);
    Task<ResultDto<PaymentDto>> GetForOrderAsync(Guid orderId, string? currentMobile, CancellationToken cancellationToken = default);
    Task<ResultDto<PaymentReceiptDto>> VerifyZarinpalAsync(Guid paymentId, string? authority, string? gatewayStatus, CancellationToken cancellationToken = default);
    Task<ResultDto<AdminPaymentsDto>> AdminListAsync(CancellationToken cancellationToken = default);
    Task<ResultDto<PaymentDto>> AdminUpdateStatusAsync(Guid paymentId, UpdatePaymentStatusRequest request, string changedBy, CancellationToken cancellationToken = default);
    Task<ResultDto<PaymentRefundDto>> RefundZarinpalAsync(Guid paymentId, CreateZarinpalRefundRequest request, string changedBy, CancellationToken cancellationToken = default);
}

public interface ISecurityAdminService
{
    Task<ResultDto<IReadOnlyCollection<PermissionDto>>> GetPermissionsAsync(CancellationToken cancellationToken = default);
    Task<ResultDto<IReadOnlyCollection<RoleSecurityDto>>> GetRolesAsync(CancellationToken cancellationToken = default);
    Task<ResultDto<RoleSecurityDto>> CreateRoleAsync(UpsertRoleRequest request, CancellationToken cancellationToken = default);
    Task<ResultDto<RoleSecurityDto>> UpdateRolePermissionsAsync(Guid roleId, AssignRolePermissionsRequest request, CancellationToken cancellationToken = default);
    Task<ResultDto<IReadOnlyCollection<AdminUserDto>>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<ResultDto<AdminUserDto>> CreateAdminUserAsync(CreateAdminUserRequest request, CancellationToken cancellationToken = default);
    Task<ResultDto<AdminUserDto>> UpdateUserRolesAsync(Guid userId, AssignUserRolesRequest request, CancellationToken cancellationToken = default);
    Task<ResultDto<IReadOnlyCollection<LoginAuditDto>>> GetLoginAuditsAsync(CancellationToken cancellationToken = default);
    Task<ResultDto<IReadOnlyCollection<AdminPageAccessDto>>> GetAdminPagesAsync(CancellationToken cancellationToken = default);
    Task<ResultDto<AdminPageAccessDto>> UpsertAdminPageAsync(Guid id, UpsertAdminPageAccessRequest request, CancellationToken cancellationToken = default);
}

public interface ICartPersistenceService
{
    Task<ResultDto<CartMergeResultDto>> MergeAsync(
        MergeCartRequest request,
        CartCustomerContext customer,
        CancellationToken cancellationToken = default);

    Task<ResultDto> ClearAsync(
        CartCustomerContext customer,
        CancellationToken cancellationToken = default);
}

public interface ILocationService
{
    Task<ResultDto<IReadOnlyCollection<ProvinceLocationDto>>> GetProvincesAsync(CancellationToken cancellationToken = default);
    Task<ResultDto<IReadOnlyCollection<CityLocationDto>>> GetCitiesAsync(string province, CancellationToken cancellationToken = default);
}

