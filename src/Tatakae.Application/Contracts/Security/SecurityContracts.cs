using System.ComponentModel.DataAnnotations;

namespace Tatakae.Application.Contracts.Security;

public sealed record PermissionDto(Guid Id, int PermissionNumber, string Key, string DisplayName, string PagePath, string GroupName, string Description, bool IsActive, int SortOrder);
public sealed record RoleSecurityDto(Guid Id, string Name, string DisplayName, string? Description, bool IsSystem, IReadOnlyCollection<string> Permissions);
public sealed record AdminUserDto(Guid Id, string FullName, string Mobile, string? Email, bool IsActive, bool MobileConfirmed, DateTimeOffset CreatedAt, IReadOnlyCollection<string> Roles, IReadOnlyCollection<string> Permissions);
public sealed record AdminPageAccessDto(Guid Id, string PageKey, string Title, string Path, string RequiredPermissionKey, string MenuGroup, string Icon, string Description, bool ShowInMenu, bool IsActive, int SortOrder);
public sealed record LoginAuditDto(Guid Id, Guid? UserId, string Mobile, string? FullName, string SessionKey, bool Succeeded, string? FailureReason, string? IpAddress, string? UserAgent, DateTimeOffset LoggedInAt, DateTimeOffset? TokenExpiresAt, DateTimeOffset? LastSeenAt, DateTimeOffset? LogoutAt);

public sealed class UpsertRoleRequest
{
    [Required, StringLength(80, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(120, MinimumLength = 2)]
    public string DisplayName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }
}

public sealed class AssignRolePermissionsRequest
{
    [Required(ErrorMessage = "فهرست دسترسی‌ها الزامی است.")]
    [MinLength(1, ErrorMessage = "حداقل یک Permission برای نقش انتخاب کنید.")]
    public List<string> Permissions { get; set; } = [];
}

public sealed class AssignUserRolesRequest
{
    [Required(ErrorMessage = "فهرست نقش‌ها الزامی است.")]
    [MinLength(1, ErrorMessage = "حداقل یک Role برای کاربر انتخاب کنید.")]
    public List<string> Roles { get; set; } = [];
}

public sealed class CreateAdminUserRequest
{
    [Required, StringLength(120, MinimumLength = 3)]
    public string FullName { get; set; } = string.Empty;

    [Required, RegularExpression(Tatakae.Application.Validation.IranianValidationPatterns.Mobile, ErrorMessage = "شماره موبایل ایران معتبر نیست.")]
    public string Mobile { get; set; } = string.Empty;

    [EmailAddress, StringLength(260)]
    public string? Email { get; set; }

    [Required, StringLength(80, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    public List<string> Roles { get; set; } = [];
}


public sealed class UpsertAdminPageAccessRequest
{
    [Required(ErrorMessage = "کلید صفحه الزامی است."), RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$", ErrorMessage = "کلید صفحه فقط شامل حروف کوچک انگلیسی، عدد و خط تیره است.")]
    [StringLength(120)]
    public string PageKey { get; set; } = string.Empty;

    [Required(ErrorMessage = "عنوان صفحه الزامی است."), StringLength(180, MinimumLength = 2, ErrorMessage = "عنوان صفحه باید بین ۲ تا ۱۸۰ کاراکتر باشد.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "مسیر صفحه الزامی است."), StringLength(260, MinimumLength = 2, ErrorMessage = "مسیر صفحه باید بین ۲ تا ۲۶۰ کاراکتر باشد.")]
    [RegularExpression("^/.*", ErrorMessage = "مسیر صفحه باید با / شروع شود.")]
    public string Path { get; set; } = string.Empty;

    [Required, StringLength(160)]
    public string RequiredPermissionKey { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string MenuGroup { get; set; } = string.Empty;

    [Required, StringLength(20)]
    public string Icon { get; set; } = "•";

    [StringLength(600)]
    public string Description { get; set; } = string.Empty;

    public bool ShowInMenu { get; set; } = true;
    public bool IsActive { get; set; } = true;

    [Range(0, 9999)]
    public int SortOrder { get; set; }
}
