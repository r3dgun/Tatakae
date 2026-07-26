using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Security;

namespace Tatakae.Application.Interfaces.Services;

/// <summary>Application use case for checking effective user permissions.</summary>
public interface IPermissionService
{
    Task<ResultDto<PermissionCheckResult>> CheckPermissionByInsuranceNumberAsync(
        string insuranceNumber,
        long permissionId,
        CancellationToken cancellationToken = default);
}
