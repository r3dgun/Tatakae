using Microsoft.AspNetCore.Mvc;
using Tatakae.Api.Controllers;
using Tatakae.Api.Filters;
using Tatakae.Application.Security;

namespace Tatakae.Api.Tests;

public sealed class SeoControllerRouteTests
{
    [Fact]
    public void AdminSeoController_UsesAdminSeoRouteAndPermission()
    {
        var type = typeof(AdminSeoController);
        var route = Assert.IsType<RouteAttribute>(Attribute.GetCustomAttribute(type, typeof(RouteAttribute)));
        var permission = Assert.IsType<PermissionCheckerAttribute>(Attribute.GetCustomAttribute(type, typeof(PermissionCheckerAttribute)));

        Assert.Equal("api/admin/seo", route.Template);
        Assert.NotNull(permission);
        Assert.Contains(PermissionNames.AdminSeoView, PermissionNames.All);
    }


    [Fact]
    public void StorePagesController_ExposesPublicLegalAndContactRoutes()
    {
        var type = typeof(StorePagesController);
        var route = Assert.IsType<RouteAttribute>(Attribute.GetCustomAttribute(type, typeof(RouteAttribute)));
        Assert.Equal("api/store-pages", route.Template);

        var pageMethod = type.GetMethod("Page");
        var pageRoute = Assert.IsType<HttpGetAttribute>(Attribute.GetCustomAttribute(pageMethod!, typeof(HttpGetAttribute)));
        Assert.Equal("{slug}", pageRoute.Template);

        var contactMethod = type.GetMethod("Contact");
        var contactRoute = Assert.IsType<HttpPostAttribute>(Attribute.GetCustomAttribute(contactMethod!, typeof(HttpPostAttribute)));
        Assert.Equal("contact", contactRoute.Template);
    }

    [Fact]
    public void AdminLegalController_UsesLegalRouteAndManagePermissionsOnWriteActions()
    {
        var type = typeof(AdminLegalController);
        var route = Assert.IsType<RouteAttribute>(Attribute.GetCustomAttribute(type, typeof(RouteAttribute)));
        Assert.Equal("api/admin/legal", route.Template);
        Assert.IsType<PermissionCheckerAttribute>(Attribute.GetCustomAttribute(type, typeof(PermissionCheckerAttribute)));

        var upsert = type.GetMethod("UpsertPage");
        Assert.NotNull(upsert);
        Assert.IsType<PermissionCheckerAttribute>(Attribute.GetCustomAttribute(upsert!, typeof(PermissionCheckerAttribute)));
        var put = Assert.IsType<HttpPutAttribute>(Attribute.GetCustomAttribute(upsert!, typeof(HttpPutAttribute)));
        Assert.Equal("pages/{slug}", put.Template);

        var updateMessage = type.GetMethod("UpdateContactMessage");
        Assert.NotNull(updateMessage);
        Assert.IsType<PermissionCheckerAttribute>(Attribute.GetCustomAttribute(updateMessage!, typeof(PermissionCheckerAttribute)));
        var patch = Assert.IsType<HttpPatchAttribute>(Attribute.GetCustomAttribute(updateMessage!, typeof(HttpPatchAttribute)));
        Assert.Equal("contact-messages/{id:guid}", patch.Template);

        Assert.Contains(PermissionNames.AdminLegalView, PermissionNames.All);
        Assert.Contains(PermissionNames.AdminLegalManage, PermissionNames.All);
    }

    [Fact]
    public void SitemapController_ExposesSitemapXmlRoute()
    {
        var method = typeof(SitemapController).GetMethod("Get");
        Assert.NotNull(method);
        var route = Assert.IsType<HttpGetAttribute>(Attribute.GetCustomAttribute(method!, typeof(HttpGetAttribute)));

        Assert.Equal("/sitemap.xml", route.Template);
    }

    [Fact]
    public void RobotsController_ExposesRobotsTxtRoute()
    {
        var method = typeof(RobotsController).GetMethod("Get");
        Assert.NotNull(method);
        var route = Assert.IsType<HttpGetAttribute>(Attribute.GetCustomAttribute(method!, typeof(HttpGetAttribute)));

        Assert.Equal("/robots.txt", route.Template);
    }
    [Fact]
    public void AiSeoController_ExposesLlmsAndCatalogRoutes()
    {
        var type = typeof(AiSeoController);

        var llms = type.GetMethod("Llms");
        var llmsRoute = Assert.IsType<HttpGetAttribute>(Attribute.GetCustomAttribute(llms!, typeof(HttpGetAttribute)));
        Assert.Equal("/llms.txt", llmsRoute.Template);

        var full = type.GetMethod("LlmsFull");
        var fullRoute = Assert.IsType<HttpGetAttribute>(Attribute.GetCustomAttribute(full!, typeof(HttpGetAttribute)));
        Assert.Equal("/llms-full.txt", fullRoute.Template);

        var catalog = type.GetMethod("Catalog");
        var catalogRoute = Assert.IsType<HttpGetAttribute>(Attribute.GetCustomAttribute(catalog!, typeof(HttpGetAttribute)));
        Assert.Equal("/ai/catalog.json", catalogRoute.Template);
    }

}
