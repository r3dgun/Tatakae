using Microsoft.Extensions.Logging;
using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Contracts.Security;
using Tatakae.Application.Interfaces.Gateways;
using Tatakae.Application.Interfaces.Services;

namespace Tatakae.Application.Services;

public sealed class SecurityAdminService(
    ISecurityAdminGateway gateway,
    ILogger<SecurityAdminService> logger) : ISecurityAdminService
{
    public Task<ResultDto<IReadOnlyCollection<PermissionDto>>> GetPermissionsAsync(CancellationToken cancellationToken = default)
        => ApplicationServiceResult.ExecuteAsync(
            () => gateway.GetPermissionsAsync(cancellationToken),
            "دسترسی‌ها با موفقیت دریافت شدند.",
            "خطایی در دریافت دسترسی‌ها رخ داده است.",
            "security_permissions_get_failed",
            logger);

    public Task<ResultDto<IReadOnlyCollection<RoleSecurityDto>>> GetRolesAsync(CancellationToken cancellationToken = default)
        => ApplicationServiceResult.ExecuteAsync(
            () => gateway.GetRolesAsync(cancellationToken),
            "نقش‌ها با موفقیت دریافت شدند.",
            "خطایی در دریافت نقش‌ها رخ داده است.",
            "security_roles_get_failed",
            logger);

    public Task<ResultDto<RoleSecurityDto>> CreateRoleAsync(UpsertRoleRequest request, CancellationToken cancellationToken = default)
        => request is null
            ? Task.FromResult(new ResultDto<RoleSecurityDto>().ValidationFailed("اطلاعات نقش ارسال نشده است.", "security_role_request_required"))
            : ApplicationServiceResult.ExecuteAsync(
                () => gateway.CreateRoleAsync(request, cancellationToken),
                "نقش با موفقیت ایجاد شد.",
                "خطایی در ایجاد نقش رخ داده است.",
                "security_role_create_failed",
                logger);

    public Task<ResultDto<RoleSecurityDto>> UpdateRolePermissionsAsync(Guid roleId, AssignRolePermissionsRequest request, CancellationToken cancellationToken = default)
        => roleId == Guid.Empty
            ? Task.FromResult(new ResultDto<RoleSecurityDto>().ValidationFailed("شناسه نقش معتبر نیست.", "security_role_id_invalid"))
            : request is null
                ? Task.FromResult(new ResultDto<RoleSecurityDto>().ValidationFailed("اطلاعات دسترسی‌های نقش ارسال نشده است.", "security_role_permissions_request_required"))
                : ApplicationServiceResult.ExecuteAsync(
                    () => gateway.UpdateRolePermissionsAsync(roleId, request, cancellationToken),
                    "دسترسی‌های نقش با موفقیت به‌روزرسانی شدند.",
                    "خطایی در به‌روزرسانی دسترسی‌های نقش رخ داده است.",
                    "security_role_permissions_update_failed",
                    logger);

    public Task<ResultDto<IReadOnlyCollection<AdminUserDto>>> GetUsersAsync(CancellationToken cancellationToken = default)
        => ApplicationServiceResult.ExecuteAsync(
            () => gateway.GetUsersAsync(cancellationToken),
            "کاربران مدیریت با موفقیت دریافت شدند.",
            "خطایی در دریافت کاربران مدیریت رخ داده است.",
            "security_users_get_failed",
            logger);

    public Task<ResultDto<AdminUserDto>> CreateAdminUserAsync(CreateAdminUserRequest request, CancellationToken cancellationToken = default)
        => request is null
            ? Task.FromResult(new ResultDto<AdminUserDto>().ValidationFailed("اطلاعات کاربر مدیریت ارسال نشده است.", "security_admin_user_request_required"))
            : ApplicationServiceResult.ExecuteAsync(
                () => gateway.CreateAdminUserAsync(request, cancellationToken),
                "کاربر مدیریت با موفقیت ایجاد شد.",
                "خطایی در ایجاد کاربر مدیریت رخ داده است.",
                "security_admin_user_create_failed",
                logger);

    public Task<ResultDto<AdminUserDto>> UpdateUserRolesAsync(Guid userId, AssignUserRolesRequest request, CancellationToken cancellationToken = default)
        => userId == Guid.Empty
            ? Task.FromResult(new ResultDto<AdminUserDto>().ValidationFailed("شناسه کاربر معتبر نیست.", "security_user_id_invalid"))
            : request is null
                ? Task.FromResult(new ResultDto<AdminUserDto>().ValidationFailed("اطلاعات نقش‌های کاربر ارسال نشده است.", "security_user_roles_request_required"))
                : ApplicationServiceResult.ExecuteAsync(
                    () => gateway.UpdateUserRolesAsync(userId, request, cancellationToken),
                    "نقش‌های کاربر با موفقیت به‌روزرسانی شدند.",
                    "خطایی در به‌روزرسانی نقش‌های کاربر رخ داده است.",
                    "security_user_roles_update_failed",
                    logger);

    public Task<ResultDto<IReadOnlyCollection<LoginAuditDto>>> GetLoginAuditsAsync(CancellationToken cancellationToken = default)
        => ApplicationServiceResult.ExecuteAsync(
            () => gateway.GetLoginAuditsAsync(cancellationToken),
            "گزارش ورودها با موفقیت دریافت شد.",
            "خطایی در دریافت گزارش ورودها رخ داده است.",
            "security_login_audits_get_failed",
            logger);

    public Task<ResultDto<IReadOnlyCollection<AdminPageAccessDto>>> GetAdminPagesAsync(CancellationToken cancellationToken = default)
        => ApplicationServiceResult.ExecuteAsync(
            () => gateway.GetAdminPagesAsync(cancellationToken),
            "صفحات مدیریت با موفقیت دریافت شدند.",
            "خطایی در دریافت صفحات مدیریت رخ داده است.",
            "security_admin_pages_get_failed",
            logger);

    public Task<ResultDto<AdminPageAccessDto>> UpsertAdminPageAsync(Guid id, UpsertAdminPageAccessRequest request, CancellationToken cancellationToken = default)
        => request is null
            ? Task.FromResult(new ResultDto<AdminPageAccessDto>().ValidationFailed("اطلاعات صفحه مدیریت ارسال نشده است.", "security_admin_page_request_required"))
            : ApplicationServiceResult.ExecuteAsync(
                () => gateway.UpsertAdminPageAsync(id, request, cancellationToken),
                "صفحه مدیریت با موفقیت ذخیره شد.",
                "خطایی در ذخیره صفحه مدیریت رخ داده است.",
                "security_admin_page_save_failed",
                logger);
}
