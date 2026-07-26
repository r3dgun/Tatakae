using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tatakae.Api.Controllers;
using Tatakae.Api.Filters;
using Tatakae.Application.Security;

namespace Tatakae.Api.Tests;

public sealed class NotificationControllerRouteTests
{
    [Fact]
    public void NotificationsController_UsesCustomerNotificationRouteAndAuthorization()
    {
        var type = typeof(NotificationsController);
        Assert.NotNull(Attribute.GetCustomAttribute(type, typeof(AuthorizeAttribute)));
        var route = Assert.IsType<RouteAttribute>(Attribute.GetCustomAttribute(type, typeof(RouteAttribute)));
        Assert.Equal("api/account/notifications", route.Template);
    }

    [Fact]
    public void AdminNotificationsController_UsesAdminRouteAndPermissionChecker()
    {
        var type = typeof(AdminNotificationsController);
        var route = Assert.IsType<RouteAttribute>(Attribute.GetCustomAttribute(type, typeof(RouteAttribute)));
        Assert.Equal("api/admin/notifications", route.Template);
        var permission = Assert.IsType<PermissionCheckerAttribute>(Attribute.GetCustomAttribute(type, typeof(PermissionCheckerAttribute)));
        Assert.NotNull(permission);
    }

    [Fact]
    public void PermissionCatalog_ContainsNotificationPermissionsAndAdminPage()
    {
        Assert.Contains(PermissionNames.AdminNotificationsView, PermissionNames.All);
        Assert.Contains(PermissionNames.AdminNotificationsManage, PermissionNames.All);
        Assert.Contains(AdminPermissionCatalog.All, x => x.Key == PermissionNames.AdminNotificationsView && x.PagePath == "/admin/notifications");
        Assert.Contains(AdminPageAccessCatalog.All, x => x.PageKey == "notifications" && x.Path == "/admin/notifications");
    }
}
