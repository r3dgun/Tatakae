using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tatakae.Api.Controllers;

namespace Tatakae.Api.Tests;

public sealed class WishlistControllerRouteTests
{
    [Fact]
    public void WishlistController_IsAuthorizedAndUsesAccountWishlistRoute()
    {
        var type = typeof(WishlistController);

        Assert.NotNull(Attribute.GetCustomAttribute(type, typeof(AuthorizeAttribute)));
        var route = Assert.IsType<RouteAttribute>(Attribute.GetCustomAttribute(type, typeof(RouteAttribute)));
        Assert.Equal("api/account/wishlist", route.Template);
    }

    [Fact]
    public void RecommendationsController_ExposesPublicSimilarProductsRoute()
    {
        var type = typeof(RecommendationsController);
        var route = Assert.IsType<RouteAttribute>(Attribute.GetCustomAttribute(type, typeof(RouteAttribute)));

        Assert.Equal("api/recommendations", route.Template);
    }
}
