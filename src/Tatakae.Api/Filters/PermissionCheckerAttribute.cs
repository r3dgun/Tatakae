using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Tatakae.Application.Interfaces.Services;
using Tatakae.Application.Security;

namespace Tatakae.Api.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class PermissionCheckerAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly long _permissionId;

    public PermissionCheckerAttribute(int permissionID)
    {
        _permissionId = permissionID;
    }

    public PermissionCheckerAttribute(long permissionId)
    {
        _permissionId = permissionId;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var permissionService = context.HttpContext.RequestServices.GetService<IPermissionService>();
        if (permissionService is null)
        {
            context.Result = new ForbidResult();
            return;
        }

        var user = context.HttpContext.User;

        // اگر لاگین نکرده
        if (user?.Identity?.IsAuthenticated != true)
        {
            context.Result = new RedirectResult("/login");
            return;
        }

        // گرفتن اطلاعات کاربر از Claims
        // در این پروژه ClaimTypes.Name را موبایل/شناسه ورود قرار داده‌ایم تا مثل نمونه خودت بتوانیم با آن permission را چک کنیم.
        var insuranceNumber = user.FindFirst(ClaimTypes.Name)?.Value
            ?? user.FindFirst("mobile")?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(insuranceNumber))
        {
            context.Result = new RedirectResult("/login");
            return;
        }

        // بررسی سطح دسترسی
        var checkRes = await permissionService.CheckPermissionByInsuranceNumberAsync(insuranceNumber, _permissionId, context.HttpContext.RequestAborted);

        if (!checkRes.IsSuccess || checkRes.Data is null || !checkRes.Data.IsSuccess)
        {
            // اگر دسترسی ندارد
            context.Result = new ForbidResult();
        }
    }
}
