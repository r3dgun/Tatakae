using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Tatakae.Infrastructure.Persistence.Models;

[Index(nameof(PhoneNumber), IsUnique = true)]
[Index(nameof(CustomerId))]
[Index(nameof(SellerId))]
public sealed class ApplicationUserIdentity : IdentityUser<Guid>
{
    public Guid? CustomerId { get; set; }
    public Guid? SellerId { get; set; }

    [Required, MaxLength(180)]
    public string FullName { get; set; } = string.Empty;

    public bool MobileConfirmed { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastLoginAt { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public CustomerDbRecord? Customer { get; set; }

    [ForeignKey(nameof(SellerId))]
    public SellerDbRecord? Seller { get; set; }
}

[Index(nameof(Name), IsUnique = true)]
public sealed class ApplicationRoleIdentity : IdentityRole<Guid>
{
    [Required, MaxLength(120)]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsSystem { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [InverseProperty(nameof(AppRolePermissionDbRecord.Role))]
    public List<AppRolePermissionDbRecord> Permissions { get; set; } = [];
}

[Table("AppPermissions")]
[Index(nameof(Key), IsUnique = true)]
[Index(nameof(PagePath))]
[Index(nameof(GroupName))]
public sealed class AppPermissionDbRecord : BaseEntity<Guid>
{
    public int PermissionNumber { get; set; }

    [Required, MaxLength(160)]
    public string Key { get; set; } = string.Empty;

    [Required, MaxLength(180)]
    public string DisplayName { get; set; } = string.Empty;

    [Required, MaxLength(260)]
    public string PagePath { get; set; } = string.Empty;

    [Required, MaxLength(90)]
    public string GroupName { get; set; } = string.Empty;

    [MaxLength(700)]
    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [InverseProperty(nameof(AppRolePermissionDbRecord.Permission))]
    public List<AppRolePermissionDbRecord> Roles { get; set; } = [];
}

[Table("AppRolePermissions")]
[Index(nameof(RoleId), nameof(PermissionId), IsUnique = true)]
public sealed class AppRolePermissionDbRecord : BaseEntity<Guid>
{
    [Required]
    public Guid RoleId { get; set; }

    [Required]
    public Guid PermissionId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [ForeignKey(nameof(RoleId))]
    public ApplicationRoleIdentity? Role { get; set; }

    [ForeignKey(nameof(PermissionId))]
    public AppPermissionDbRecord? Permission { get; set; }
}


[Table("AdminPageAccesses")]
[Index(nameof(PageKey), IsUnique = true)]
[Index(nameof(Path), IsUnique = true)]
[Index(nameof(RequiredPermissionKey))]
public sealed class AdminPageAccessDbRecord : BaseEntity<Guid>
{
    [Required, MaxLength(120)]
    public string PageKey { get; set; } = string.Empty;

    [Required, MaxLength(180)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(260)]
    public string Path { get; set; } = string.Empty;

    [Required, MaxLength(160)]
    public string RequiredPermissionKey { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string MenuGroup { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Icon { get; set; } = "•";

    [MaxLength(600)]
    public string Description { get; set; } = string.Empty;

    public bool ShowInMenu { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
