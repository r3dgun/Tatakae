using Microsoft.AspNetCore.Mvc;
using Tatakae.Api.Controllers;
using Tatakae.Api.Filters;

namespace Tatakae.Api.Tests;

public sealed class AdminDashboardRouteTests
{
    [Fact]
    public void AdminDashboardController_UsesAdminDashboardRouteAndPermission()
    {
        var type = typeof(AdminDashboardController);
        var route = Assert.IsType<RouteAttribute>(Attribute.GetCustomAttribute(type, typeof(RouteAttribute)));
        Assert.NotNull(Attribute.GetCustomAttribute(type, typeof(PermissionCheckerAttribute)));

        Assert.Equal("api/admin/dashboard", route.Template);
    }
}
