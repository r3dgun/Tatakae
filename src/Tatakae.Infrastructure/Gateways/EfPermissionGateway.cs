using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Tatakae.Application.Security;
using Tatakae.Application.Interfaces.Gateways;
using Tatakae.Infrastructure.Persistence;
using Tatakae.Infrastructure.Persistence.Models;

namespace Tatakae.Infrastructure.Gateways;

public sealed class EfPermissionGateway(
    TatakaeDbContext db,
    UserManager<ApplicationUserIdentity> users,
    RoleManager<ApplicationRoleIdentity> roles) : IPermissionGateway
{
    public async Task<PermissionCheckResult?> CheckAsync(string insuranceNumber, long permissionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(insuranceNumber) || permissionId <= 0)
            return new PermissionCheckResult(false, "شناسه کاربر یا PermissionId معتبر نیست.");

        var normalized = NormalizeIdentifier(insuranceNumber);

        // روش اصلی مطابق مدل‌هایی که خواستی: User -> UserRole -> Role -> PermissionsRole -> Permission
        var hasCustomPermission = await db.PermissionUsers
            .Where(u => u.IsActive && (u.InsuranceNumber == normalized || u.Mobile == normalized || u.UserName == normalized || u.FullName == insuranceNumber))
            .Join(db.PermissionUserRoles, u => u.UserId, ur => ur.UserId, (u, ur) => ur)
            .Join(db.PermissionRoles.Where(r => r.IsActive), ur => ur.RoleId, r => r.RoleId, (ur, r) => r)
            .Join(db.PermissionsRoles, r => r.RoleId, rp => rp.RoleId, (r, rp) => rp)
            .Join(db.PermissionDefinitions.Where(p => p.IsActive), rp => rp.PermissionId, p => p.PermissionId, (rp, p) => p)
            .AnyAsync(p => p.PermissionId == permissionId, cancellationToken);

        if (hasCustomPermission)
            return new PermissionCheckResult(true);

        // Fallback برای اینکه اگر جدول‌های PermissionChecker هنوز sync نشده بودند، Identity هم مستقیم چک شود.
        var identityUser = await FindIdentityUserAsync(normalized, insuranceNumber);
        if (identityUser is null || !identityUser.IsActive)
            return new PermissionCheckResult(false, "کاربر پیدا نشد یا غیرفعال است.");

        var roleNames = await users.GetRolesAsync(identityUser);
        if (roleNames.Count == 0)
            return new PermissionCheckResult(false, "کاربر هیچ نقشی ندارد.");

        var roleIds = await roles.Roles
            .Where(r => r.Name != null && roleNames.Contains(r.Name))
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        var permissionKey = PermissionIds.ToKey(permissionId);
        var hasIdentityPermission = await db.RolePermissions
            .Include(x => x.Permission)
            .AnyAsync(x =>
                roleIds.Contains(x.RoleId) &&
                x.Permission != null &&
                x.Permission.IsActive &&
                (x.Permission.PermissionNumber == permissionId || x.Permission.Key == permissionKey), cancellationToken);

        return hasIdentityPermission
            ? new PermissionCheckResult(true)
            : new PermissionCheckResult(false, "کاربر Permission لازم را ندارد.");
    }

    private async Task<ApplicationUserIdentity?> FindIdentityUserAsync(string normalized, string original)
    {
        if (Guid.TryParse(normalized, out var userId))
        {
            var byId = await users.FindByIdAsync(userId.ToString());
            if (byId is not null) return byId;
        }

        return await users.FindByNameAsync(normalized)
            ?? await users.Users.FirstOrDefaultAsync(x =>
                x.PhoneNumber == normalized ||
                x.UserName == normalized ||
                x.FullName == original);
    }

    private static string NormalizeIdentifier(string value)
    {
        var result = value.Trim().Replace(" ", "").Replace("-", "");
        if (result.StartsWith("+98", StringComparison.Ordinal)) result = "0" + result[3..];
        if (result.StartsWith("98", StringComparison.Ordinal) && result.Length == 12) result = "0" + result[2..];
        return result;
    }
}
