using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tatakae.Infrastructure.Persistence.Models;

[Table("PermissionUsers")]
[Index(nameof(IdentityUserId), IsUnique = true)]
[Index(nameof(InsuranceNumber), IsUnique = true)]
[Index(nameof(Mobile), IsUnique = true)]
public class User : BaseEntity<long>
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long UserId { get; set; }

    public Guid? IdentityUserId { get; set; }

    [Required, MaxLength(120)]
    public string InsuranceNumber { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string UserName { get; set; } = string.Empty;

    [Required, MaxLength(20), Phone]
    public string Mobile { get; set; } = string.Empty;

    [Required, MaxLength(180)]
    public string FullName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    #region Relations
    public virtual List<UserRole> UserRoles { get; set; } = [];
    #endregion
}

[Table("PermissionRoles")]
[Index(nameof(Name), IsUnique = true)]
public class Role : BaseEntity<long>
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long RoleId { get; set; }

    public Guid? IdentityRoleId { get; set; }

    [Required, MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsSystem { get; set; }
    public bool IsActive { get; set; } = true;

    #region Relations
    public virtual List<UserRole> UserRoles { get; set; } = [];
    public virtual List<PermissionsRole> PermissionsRoles { get; set; } = [];
    #endregion
}

[Table("PermissionDefinitions")]
[Index(nameof(Key), IsUnique = true)]
[Index(nameof(PagePath))]
public class Permission : BaseEntity<long>
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public long PermissionId { get; set; }

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

    #region Relations
    public virtual List<PermissionsRole> PermissionsRoles { get; set; } = [];
    #endregion
}

[Table("UserRoles")]
[Index(nameof(UserId), nameof(RoleId), IsUnique = true)]
public class UserRole : BaseEntity<long>
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long UR_Id { get; set; }
    public long UserId { get; set; }
    public long RoleId { get; set; }

    #region Relations
    [ForeignKey(nameof(UserId))]
    public virtual User? User { get; set; }

    [ForeignKey(nameof(RoleId))]
    public virtual Role Role { get; set; } = default!;
    #endregion
}

[Table("PermissionsRoles")]
[Index(nameof(RoleId), nameof(PermissionId), IsUnique = true)]
public class PermissionsRole : BaseEntity<long>
{
    public PermissionsRole()
    {

    }

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long RP_Id { get; set; }
    public long RoleId { get; set; }
    public long PermissionId { get; set; }

    #region Relations
    [ForeignKey(nameof(PermissionId))]
    public virtual Permission Permission { get; set; } = default!;

    [ForeignKey(nameof(RoleId))]
    public virtual Role Role { get; set; } = default!;
    #endregion
}
