using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tatakae.Api.Controllers;
using Tatakae.Api.Filters;

namespace Tatakae.Api.Tests;

public sealed class ArtworkControllerRouteTests
{
    [Fact]
    public void ArtworksController_UsesAccountArtworksRouteAndAuthorization()
    {
        var type = typeof(ArtworksController);
        Assert.NotNull(Attribute.GetCustomAttribute(type, typeof(AuthorizeAttribute)));
        var route = Assert.IsType<RouteAttribute>(Attribute.GetCustomAttribute(type, typeof(RouteAttribute)));
        Assert.Equal("api/account/artworks", route.Template);
    }

    [Fact]
    public void AdminArtworksController_UsesAdminArtworksRouteAndPermissionChecker()
    {
        var type = typeof(AdminArtworksController);
        Assert.NotNull(Attribute.GetCustomAttribute(type, typeof(PermissionCheckerAttribute)));
        var route = Assert.IsType<RouteAttribute>(Attribute.GetCustomAttribute(type, typeof(RouteAttribute)));
        Assert.Equal("api/admin/artworks", route.Template);
    }
}
