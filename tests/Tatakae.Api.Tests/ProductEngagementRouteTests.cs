using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tatakae.Api.Controllers;

namespace Tatakae.Api.Tests;

public sealed class ProductEngagementRouteTests
{
    [Fact]
    public void AdminReviewsController_UsesAdminReviewsRouteAndAuthorization()
    {
        var type = typeof(AdminReviewsController);
        Assert.NotNull(Attribute.GetCustomAttribute(type, typeof(AuthorizeAttribute)));
        var route = Assert.IsType<RouteAttribute>(Attribute.GetCustomAttribute(type, typeof(RouteAttribute)));
        Assert.Equal("api/admin/reviews", route.Template);
    }

    [Fact]
    public void AdminQuestionsController_UsesAdminQuestionsRouteAndAuthorization()
    {
        var type = typeof(AdminQuestionsController);
        Assert.NotNull(Attribute.GetCustomAttribute(type, typeof(AuthorizeAttribute)));
        var route = Assert.IsType<RouteAttribute>(Attribute.GetCustomAttribute(type, typeof(RouteAttribute)));
        Assert.Equal("api/admin/questions", route.Template);
    }
}
