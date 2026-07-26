using Microsoft.AspNetCore.Identity;
using Tatakae.Application.Interfaces.Gateways;
using Microsoft.EntityFrameworkCore;
using Tatakae.Application.Contracts.Security;
using Tatakae.Application.Security;
using Tatakae.Infrastructure.Persistence;
using Tatakae.Infrastructure.Persistence.Models;

namespace Tatakae.Infrastructure.Gateways;

public sealed class EfSecurityAdminGateway(
    UserManager<ApplicationUserIdentity> users,
    RoleManager<ApplicationRoleIdentity> roles,
    TatakaeDbContext db) : ISecurityAdminGateway
{
    public async Task<IReadOnlyCollection<PermissionDto>> GetPermissionsAsync(CancellationToken cancellationToken)
        => await db.Permissions
            .OrderBy(x => x.SortOrder)
            .Select(x => new PermissionDto(x.Id, x.PermissionNumber, x.Key, x.DisplayName, x.PagePath, x.GroupName, x.Description, x.IsActive, x.SortOrder))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<RoleSecurityDto>> GetRolesAsync(CancellationToken cancellationToken)
    {
        var allRoles = await roles.Roles.OrderBy(x => x.Name).ToListAsync(cancellationToken);
        var roleIds = allRoles.Select(x => x.Id).ToArray();
        var permissionMap = await db.RolePermissions
            .Include(x => x.Permission)
            .Where(x => roleIds.Contains(x.RoleId) && x.Permission != null)
            .GroupBy(x => x.RoleId)
            .Select(x => new { RoleId = x.Key, Permissions = x.Select(p => p.Permission!.Key).OrderBy(p => p).ToList() })
            .ToDictionaryAsync(x => x.RoleId, x => (IReadOnlyCollection<string>)x.Permissions, cancellationToken);

        return allRoles.Select(role => new RoleSecurityDto(
            role.Id,
            role.Name ?? string.Empty,
            role.DisplayName,
            role.Description,
            role.IsSystem,
            permissionMap.TryGetValue(role.Id, out var permissions) ? permissions : Array.Empty<string>())).ToList();
    }

    public async Task<RoleSecurityDto> CreateRoleAsync(UpsertRoleRequest request, CancellationToken cancellationToken)
    {
        var role = new ApplicationRoleIdentity
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            NormalizedName = request.Name.Trim().ToUpperInvariant(),
            DisplayName = request.DisplayName.Trim(),
            Description = request.Description,
            IsSystem = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var result = await roles.CreateAsync(role);
        if (!result.Succeeded) throw new InvalidOperationException(string.Join(" | ", result.Errors.Select(x => x.Description)));
        await SyncPermissionCheckerTablesAsync(cancellationToken);
        return new RoleSecurityDto(role.Id, role.Name!, role.DisplayName, role.Description, role.IsSystem, Array.Empty<string>());
    }

    public async Task<RoleSecurityDto> UpdateRolePermissionsAsync(Guid roleId, AssignRolePermissionsRequest request, CancellationToken cancellationToken)
    {
        var role = await roles.FindByIdAsync(roleId.ToString()) ?? throw new KeyNotFoundException("نقش پیدا نشد.");
        var permissionIds = await db.Permissions
            .Where(x => request.Permissions.Contains(x.Key) && x.IsActive)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var existing = await db.RolePermissions
            .IgnoreQueryFilters()
            .Where(x => x.RoleId == role.Id)
            .ToListAsync(cancellationToken);

        foreach (var row in existing.Where(x => !x.IsRemoved && !permissionIds.Contains(x.PermissionId)))
        {
            db.SoftDelete(row);
        }

        foreach (var permissionId in permissionIds)
        {
            var row = existing.SingleOrDefault(x => x.PermissionId == permissionId);
            if (row is null)
            {
                db.RolePermissions.Add(new AppRolePermissionDbRecord
                {
                    Id = Guid.NewGuid(),
                    RoleId = role.Id,
                    PermissionId = permissionId,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
            else
            {
                db.Restore(row);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        await SyncPermissionCheckerTablesAsync(cancellationToken);
        var permissions = await db.RolePermissions.Include(x => x.Permission).Where(x => x.RoleId == role.Id && x.Permission != null).Select(x => x.Permission!.Key).OrderBy(x => x).ToListAsync(cancellationToken);
        return new RoleSecurityDto(role.Id, role.Name ?? string.Empty, role.DisplayName, role.Description, role.IsSystem, permissions);
    }

    public async Task<IReadOnlyCollection<AdminUserDto>> GetUsersAsync(CancellationToken cancellationToken)
    {
        var allUsers = await users.Users.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
        var result = new List<AdminUserDto>();
        foreach (var user in allUsers)
        {
            var roleNames = await users.GetRolesAsync(user);
            var permissions = await GetUserPermissionsAsync(roleNames, cancellationToken);
            result.Add(new AdminUserDto(user.Id, user.FullName, user.PhoneNumber ?? user.UserName ?? string.Empty, user.Email, user.IsActive, user.PhoneNumberConfirmed, user.CreatedAt, roleNames.OrderBy(x => x).ToArray(), permissions));
        }
        return result;
    }

    public async Task<AdminUserDto> CreateAdminUserAsync(CreateAdminUserRequest request, CancellationToken cancellationToken)
    {
        var mobile = NormalizeMobile(request.Mobile);
        var existing = await users.FindByNameAsync(mobile);
        if (existing is not null) throw new InvalidOperationException("کاربری با این شماره قبلاً وجود دارد.");

        var user = new ApplicationUserIdentity
        {
            Id = Guid.NewGuid(),
            UserName = mobile,
            PhoneNumber = mobile,
            PhoneNumberConfirmed = true,
            MobileConfirmed = true,
            Email = request.Email,
            EmailConfirmed = !string.IsNullOrWhiteSpace(request.Email),
            FullName = request.FullName.Trim(),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            SecurityStamp = Guid.NewGuid().ToString("N")
        };

        var result = await users.CreateAsync(user, request.Password);
        if (!result.Succeeded) throw new InvalidOperationException(string.Join(" | ", result.Errors.Select(x => x.Description)));
        if (request.Roles.Count > 0) await users.AddToRolesAsync(user, request.Roles);
        await SyncPermissionCheckerTablesAsync(cancellationToken);
        var roleNames = await users.GetRolesAsync(user);
        var permissions = await GetUserPermissionsAsync(roleNames, cancellationToken);
        return new AdminUserDto(user.Id, user.FullName, user.PhoneNumber ?? user.UserName ?? string.Empty, user.Email, user.IsActive, user.PhoneNumberConfirmed, user.CreatedAt, roleNames.OrderBy(x => x).ToArray(), permissions);
    }

    public async Task<AdminUserDto> UpdateUserRolesAsync(Guid userId, AssignUserRolesRequest request, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(userId.ToString()) ?? throw new KeyNotFoundException("کاربر پیدا نشد.");
        var current = await users.GetRolesAsync(user);
        await users.RemoveFromRolesAsync(user, current);
        if (request.Roles.Count > 0) await users.AddToRolesAsync(user, request.Roles);
        await SyncPermissionCheckerTablesAsync(cancellationToken);
        var roleNames = await users.GetRolesAsync(user);
        var permissions = await GetUserPermissionsAsync(roleNames, cancellationToken);
        return new AdminUserDto(user.Id, user.FullName, user.PhoneNumber ?? user.UserName ?? string.Empty, user.Email, user.IsActive, user.PhoneNumberConfirmed, user.CreatedAt, roleNames.OrderBy(x => x).ToArray(), permissions);
    }




    public async Task<IReadOnlyCollection<LoginAuditDto>> GetLoginAuditsAsync(CancellationToken cancellationToken)
        => await db.LoginAudits.AsNoTracking()
            .OrderByDescending(x => x.LoggedInAt)
            .Take(150)
            .Select(x => new LoginAuditDto(
                x.Id,
                x.UserId,
                x.Mobile,
                x.FullName,
                x.SessionKey,
                x.Succeeded,
                x.FailureReason,
                x.IpAddress,
                x.UserAgent,
                x.LoggedInAt,
                x.TokenExpiresAt,
                x.LastSeenAt,
                x.LogoutAt))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<AdminPageAccessDto>> GetAdminPagesAsync(CancellationToken cancellationToken)
        => await db.AdminPageAccesses.AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Title)
            .Select(x => new AdminPageAccessDto(x.Id, x.PageKey, x.Title, x.Path, x.RequiredPermissionKey, x.MenuGroup, x.Icon, x.Description, x.ShowInMenu, x.IsActive, x.SortOrder))
            .ToListAsync(cancellationToken);

    public async Task<AdminPageAccessDto> UpsertAdminPageAsync(Guid id, UpsertAdminPageAccessRequest request, CancellationToken cancellationToken)
    {
        if (!await db.Permissions.AnyAsync(x => x.Key == request.RequiredPermissionKey && x.IsActive, cancellationToken))
            throw new InvalidOperationException("Permission انتخاب‌شده وجود ندارد یا غیرفعال است.");

        var page = await db.AdminPageAccesses.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (page is null)
        {
            page = new AdminPageAccessDbRecord { Id = id == Guid.Empty ? Guid.NewGuid() : id };
            db.AdminPageAccesses.Add(page);
        }

        page.PageKey = request.PageKey.Trim().ToLowerInvariant();
        page.Title = request.Title.Trim();
        page.Path = NormalizeAdminPath(request.Path);
        page.RequiredPermissionKey = request.RequiredPermissionKey.Trim();
        page.MenuGroup = request.MenuGroup.Trim();
        page.Icon = request.Icon.Trim();
        page.Description = request.Description.Trim();
        page.ShowInMenu = request.ShowInMenu;
        page.IsActive = request.IsActive;
        page.SortOrder = request.SortOrder;
        page.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return new AdminPageAccessDto(page.Id, page.PageKey, page.Title, page.Path, page.RequiredPermissionKey, page.MenuGroup, page.Icon, page.Description, page.ShowInMenu, page.IsActive, page.SortOrder);
    }

    private static string NormalizeAdminPath(string path)
    {
        var value = path.Trim();
        if (!value.StartsWith('/')) value = "/" + value;
        return value;
    }

    private async Task<IReadOnlyCollection<string>> GetUserPermissionsAsync(IEnumerable<string> roleNames, CancellationToken cancellationToken)
    {
        var roleNameList = roleNames.ToArray();
        var roleIds = await roles.Roles.Where(x => x.Name != null && roleNameList.Contains(x.Name)).Select(x => x.Id).ToListAsync(cancellationToken);
        return await db.RolePermissions.Include(x => x.Permission)
            .Where(x => roleIds.Contains(x.RoleId) && x.Permission != null && x.Permission.IsActive)
            .Select(x => x.Permission!.Key)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);
    }


    private async Task SyncPermissionCheckerTablesAsync(CancellationToken cancellationToken)
    {
        foreach (var appPermission in await db.Permissions.AsNoTracking().ToListAsync(cancellationToken))
        {
            var numericId = appPermission.PermissionNumber != 0 ? appPermission.PermissionNumber : PermissionIds.FromKey(appPermission.Key);
            if (numericId == 0) continue;
            var permission = await db.PermissionDefinitions.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.PermissionId == numericId, cancellationToken);
            if (permission is null)
            {
                permission = new Tatakae.Infrastructure.Persistence.Models.Permission { PermissionId = numericId };
                db.PermissionDefinitions.Add(permission);
            }
            else
            {
                db.Restore(permission);
            }
            permission.Key = appPermission.Key;
            permission.DisplayName = appPermission.DisplayName;
            permission.PagePath = appPermission.PagePath;
            permission.GroupName = appPermission.GroupName;
            permission.Description = appPermission.Description;
            permission.SortOrder = appPermission.SortOrder;
            permission.IsActive = appPermission.IsActive;
        }

        foreach (var identityRole in await roles.Roles.AsNoTracking().ToListAsync(cancellationToken))
        {
            var role = await db.PermissionRoles.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.IdentityRoleId == identityRole.Id || x.Name == identityRole.Name, cancellationToken);
            if (role is null)
            {
                role = new Tatakae.Infrastructure.Persistence.Models.Role();
                db.PermissionRoles.Add(role);
            }
            else
            {
                db.Restore(role);
            }
            role.IdentityRoleId = identityRole.Id;
            role.Name = identityRole.Name ?? string.Empty;
            role.DisplayName = identityRole.DisplayName;
            role.Description = identityRole.Description;
            role.IsSystem = identityRole.IsSystem;
            role.IsActive = true;
        }

        await db.SaveChangesAsync(cancellationToken);

        var identityUsers = await users.Users.AsNoTracking().ToListAsync(cancellationToken);
        foreach (var identityUser in identityUsers)
        {
            var mobile = identityUser.PhoneNumber ?? identityUser.UserName ?? string.Empty;
            var user = await db.PermissionUsers.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.IdentityUserId == identityUser.Id || x.InsuranceNumber == mobile || x.Mobile == mobile, cancellationToken);
            if (user is null)
            {
                user = new Tatakae.Infrastructure.Persistence.Models.User();
                db.PermissionUsers.Add(user);
            }
            else
            {
                db.Restore(user);
            }
            user.IdentityUserId = identityUser.Id;
            user.InsuranceNumber = mobile;
            user.UserName = identityUser.UserName ?? mobile;
            user.Mobile = mobile;
            user.FullName = identityUser.FullName;
            user.IsActive = identityUser.IsActive;
        }

        await db.SaveChangesAsync(cancellationToken);

        var existingPermissionUserRoles = await db.PermissionUserRoles
            .IgnoreQueryFilters()
            .ToListAsync(cancellationToken);
        var existingPermissionsRoles = await db.PermissionsRoles
            .IgnoreQueryFilters()
            .ToListAsync(cancellationToken);
        db.SoftDeleteRange(existingPermissionUserRoles);
        db.SoftDeleteRange(existingPermissionsRoles);
        await db.SaveChangesAsync(cancellationToken);

        var customRoles = await db.PermissionRoles.AsNoTracking().ToDictionaryAsync(x => x.IdentityRoleId, x => x.RoleId, cancellationToken);
        var customUsers = await db.PermissionUsers.AsNoTracking().ToDictionaryAsync(x => x.IdentityUserId, x => x.UserId, cancellationToken);
        var identityUserRoles = await db.Set<IdentityUserRole<Guid>>().AsNoTracking().ToListAsync(cancellationToken);
        foreach (var identityUserRole in identityUserRoles)
        {
            if (!customUsers.TryGetValue(identityUserRole.UserId, out var customUserId)) continue;
            if (!customRoles.TryGetValue(identityUserRole.RoleId, out var customRoleId)) continue;
            var existingUserRole = existingPermissionUserRoles
                .SingleOrDefault(x => x.UserId == customUserId && x.RoleId == customRoleId);
            if (existingUserRole is null)
                db.PermissionUserRoles.Add(new UserRole { UserId = customUserId, RoleId = customRoleId });
            else
                db.Restore(existingUserRole);
        }

        var customRoleByIdentityId = await db.PermissionRoles.AsNoTracking().ToDictionaryAsync(x => x.IdentityRoleId, x => x.RoleId, cancellationToken);
        var appRolePermissions = await db.RolePermissions.Include(x => x.Permission).AsNoTracking().Where(x => x.Permission != null).ToListAsync(cancellationToken);
        foreach (var appRolePermission in appRolePermissions)
        {
            if (!customRoleByIdentityId.TryGetValue(appRolePermission.RoleId, out var customRoleId)) continue;
            var numericPermissionId = appRolePermission.Permission!.PermissionNumber != 0 ? appRolePermission.Permission.PermissionNumber : PermissionIds.FromKey(appRolePermission.Permission.Key);
            if (numericPermissionId == 0) continue;
            var existingPermissionRole = existingPermissionsRoles
                .SingleOrDefault(x => x.RoleId == customRoleId && x.PermissionId == numericPermissionId);
            if (existingPermissionRole is null)
                db.PermissionsRoles.Add(new PermissionsRole { RoleId = customRoleId, PermissionId = numericPermissionId });
            else
                db.Restore(existingPermissionRole);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizeMobile(string mobile)
    {
        var value = mobile.Trim().Replace(" ", "").Replace("-", "");
        if (value.StartsWith("+98", StringComparison.Ordinal)) value = "0" + value[3..];
        if (value.StartsWith("98", StringComparison.Ordinal) && value.Length == 12) value = "0" + value[2..];
        return value;
    }
}
