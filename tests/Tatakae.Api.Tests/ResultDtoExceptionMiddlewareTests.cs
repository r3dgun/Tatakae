using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Tatakae.Api.Middleware;
using Tatakae.Application.Contracts.Common;

namespace Tatakae.Api.Tests;

public sealed class ResultDtoExceptionMiddlewareTests
{
    [Theory]
    [InlineData(ResultStatus.ValidationError, StatusCodes.Status400BadRequest)]
    [InlineData(ResultStatus.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(ResultStatus.Conflict, StatusCodes.Status409Conflict)]
    [InlineData(ResultStatus.Unauthorized, StatusCodes.Status401Unauthorized)]
    [InlineData(ResultStatus.Forbidden, StatusCodes.Status403Forbidden)]
    [InlineData(ResultStatus.Failure, StatusCodes.Status500InternalServerError)]
    public void ToStatusCode_MapsSemanticResultStatus(ResultStatus status, int expected)
        => Assert.Equal(expected, ResultDtoExceptionMiddleware.ToStatusCode(status));

    [Fact]
    public async Task InvokeAsync_ResultDtoException_WritesOriginalPersianMessageAndErrorCode()
    {
        var middleware = new ResultDtoExceptionMiddleware(
            _ => throw new ResultDtoException(
                "محصول موردنظر پیدا نشد.",
                ResultStatus.NotFound,
                "product_not_found"),
            NullLogger<ResultDtoExceptionMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        var root = document.RootElement;

        Assert.False(root.GetProperty("isSuccess").GetBoolean());
        Assert.Equal("محصول موردنظر پیدا نشد.", root.GetProperty("message").GetString());
        Assert.Equal("product_not_found", root.GetProperty("errorCode").GetString());
        Assert.Equal((int)ResultStatus.NotFound, root.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task InvokeAsync_UnexpectedException_DoesNotExposeTechnicalDetails()
    {
        var middleware = new ResultDtoExceptionMiddleware(
            _ => throw new Exception("SQL password=secret"),
            NullLogger<ResultDtoExceptionMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();

        Assert.Contains("خطایی در پردازش درخواست رخ داده است.", body);
        Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", body, StringComparison.OrdinalIgnoreCase);
    }
}
