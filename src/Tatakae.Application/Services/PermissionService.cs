using Microsoft.Extensions.Logging;
using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Interfaces.Gateways;
using Tatakae.Application.Interfaces.Services;
using Tatakae.Application.Security;

namespace Tatakae.Application.Services;

public sealed class PermissionService(
    IPermissionGateway gateway,
    ILogger<PermissionService> logger) : IPermissionService
{
    public Task<ResultDto<PermissionCheckResult>> CheckPermissionByInsuranceNumberAsync(
        string insuranceNumber,
        long permissionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(insuranceNumber))
            return Task.FromResult(new ResultDto<PermissionCheckResult>().ValidationFailed("شناسه کاربر الزامی است.", "permission_user_required"));

        if (permissionId <= 0)
            return Task.FromResult(new ResultDto<PermissionCheckResult>().ValidationFailed("شناسه دسترسی معتبر نیست.", "permission_id_invalid"));

        return ApplicationServiceResult.ExecuteNullableAsync(
            () => gateway.CheckAsync(insuranceNumber, permissionId, cancellationToken),
            "نتیجه بررسی دسترسی با موفقیت دریافت شد.",
            "خطایی در بررسی دسترسی کاربر رخ داده است.",
            "permission_check_failed",
            logger,
            ResultStatus.Forbidden,
            "کاربر دسترسی لازم را ندارد.",
            "permission_denied");
    }
}
