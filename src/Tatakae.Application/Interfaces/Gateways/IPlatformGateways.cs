using Tatakae.Application.Contracts.Account;
using Tatakae.Application.Contracts.Cart;
using Tatakae.Application.Contracts.Legal;
using Tatakae.Application.Contracts.Lookups;
using Tatakae.Application.Contracts.Payments;
using Tatakae.Application.Contracts.Security;
using Tatakae.Application.Security;
using Tatakae.Domain.Enums;

namespace Tatakae.Application.Interfaces.Gateways;

/// <summary>
/// Framework-facing ports used by Application use cases. Implementations live in Infrastructure.
/// None of these contracts expose EF Core, ASP.NET Identity, JWT, HttpContext or API types.
/// </summary>
public interface IIdentityAuthGateway
{
    Task<AccountSessionDto> RegisterAsync(RegisterCustomerRequest request, ClientRequestMetadata metadata, CancellationToken cancellationToken = default);
    Task<AccountSessionDto?> LoginAsync(LoginRequest request, ClientRequestMetadata metadata, CancellationToken cancellationToken = default);
    Task<AccountSessionDto?> CurrentAsync(AuthenticatedSessionContext session, CancellationToken cancellationToken = default);
    Task LogoutAsync(AuthenticatedSessionContext session, CancellationToken cancellationToken = default);
}

public interface ILegalContentGateway
{
    Task<IReadOnlyCollection<StorePolicyPageDto>> GetPublishedPagesAsync(CancellationToken cancellationToken = default);
    Task<StorePolicyPageDto?> GetPublishedPageAsync(string slug, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<StorePolicyPageDto>> GetAllPagesAsync(CancellationToken cancellationToken = default);
    Task<StorePolicyPageDto> UpsertPageAsync(string? currentSlug, UpsertStorePolicyPageRequest request, CancellationToken cancellationToken = default);
    Task<ContactMessageDto> SubmitContactAsync(SubmitContactMessageRequest request, string? ipAddress, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ContactMessageDto>> GetContactMessagesAsync(CancellationToken cancellationToken = default);
    Task<ContactMessageDto> UpdateContactMessageAsync(Guid id, UpdateContactMessageStatusRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// External payment-provider port. Its implementation talks to Zarinpal only and
/// never reads or mutates orders or payment persistence records.
/// </summary>
public interface IZarinpalPaymentGateway
{
    string Currency { get; }
    string GetRedirectUrl(string authority);
    Task<ZarinpalRequestResult> RequestAsync(ZarinpalPaymentRequest request, CancellationToken cancellationToken = default);
    Task<ZarinpalVerifyResult> VerifyAsync(ZarinpalVerifyRequest request, CancellationToken cancellationToken = default);
    Task<ZarinpalReverseResult> ReverseAsync(ZarinpalReverseRequest request, CancellationToken cancellationToken = default);
    Task<ZarinpalRefundResult> RefundAsync(ZarinpalRefundRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Payment persistence port. It persists decisions already made by the
/// PaymentService and Order aggregate; it does not contain payment workflow rules.
/// </summary>
public interface IPaymentRepository
{
    Task<PaymentDto?> GetActiveForOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<PaymentDto?> GetForOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<PaymentDto?> GetByIdAsync(Guid paymentId, CancellationToken cancellationToken = default);
    Task<CreatePaymentResult> CreateAsync(CreatePaymentRecord command, CancellationToken cancellationToken = default);
    Task<PaymentDto> PersistOutcomeAsync(PersistPaymentOutcome command, CancellationToken cancellationToken = default);
    Task<CreatePaymentRefundResult> CreateRefundAsync(CreatePaymentRefundRecord command, CancellationToken cancellationToken = default);
    Task<PaymentRefundDto> PersistRefundOutcomeAsync(PersistPaymentRefundOutcome command, CancellationToken cancellationToken = default);
    Task<AdminPaymentsDto> AdminListAsync(CancellationToken cancellationToken = default);
}

public interface ISecurityAdminGateway
{
    Task<IReadOnlyCollection<PermissionDto>> GetPermissionsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<RoleSecurityDto>> GetRolesAsync(CancellationToken cancellationToken = default);
    Task<RoleSecurityDto> CreateRoleAsync(UpsertRoleRequest request, CancellationToken cancellationToken = default);
    Task<RoleSecurityDto> UpdateRolePermissionsAsync(Guid roleId, AssignRolePermissionsRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<AdminUserDto>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<AdminUserDto> CreateAdminUserAsync(CreateAdminUserRequest request, CancellationToken cancellationToken = default);
    Task<AdminUserDto> UpdateUserRolesAsync(Guid userId, AssignUserRolesRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<LoginAuditDto>> GetLoginAuditsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<AdminPageAccessDto>> GetAdminPagesAsync(CancellationToken cancellationToken = default);
    Task<AdminPageAccessDto> UpsertAdminPageAsync(Guid id, UpsertAdminPageAccessRequest request, CancellationToken cancellationToken = default);
}

public interface IPermissionGateway
{
    Task<PermissionCheckResult?> CheckAsync(string insuranceNumber, long permissionId, CancellationToken cancellationToken = default);
}

public interface ICartPersistenceGateway
{
    Task<CartMergeResultDto> MergeAsync(MergeCartRequest request, CartCustomerContext customer, CancellationToken cancellationToken = default);
    Task ClearAsync(CartCustomerContext customer, CancellationToken cancellationToken = default);
}

public interface ILocationGateway
{
    Task<IReadOnlyCollection<ProvinceLocationDto>> GetProvincesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<CityLocationDto>> GetCitiesAsync(string province, CancellationToken cancellationToken = default);
}
