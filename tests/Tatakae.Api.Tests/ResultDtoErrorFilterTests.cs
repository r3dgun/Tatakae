using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Tatakae.Api.Filters;
using Tatakae.Application.Contracts.Common;

namespace Tatakae.Api.Tests;

public sealed class ResultDtoErrorFilterTests
{
    [Fact]
    public async Task LegacyProblemDetails_IsConvertedToResultDto()
    {
        var actionContext = CreateActionContext();
        var filters = new List<IFilterMetadata>();
        var original = new NotFoundObjectResult(new ProblemDetails
        {
            Detail = "محصول موردنظر پیدا نشد."
        });

        var context = new ResultExecutingContext(
            actionContext,
            filters,
            original,
            new object());

        var filter = new ResultDtoErrorFilter();
        await filter.OnResultExecutionAsync(
            context,
            () => Task.FromResult(new ResultExecutedContext(
                actionContext,
                filters,
                context.Result,
                new object())));

        var objectResult = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(StatusCodes.Status404NotFound, objectResult.StatusCode);

        var result = Assert.IsType<ResultDto>(objectResult.Value);
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Equal("محصول موردنظر پیدا نشد.", result.Message);
        Assert.Equal("not_found", result.ErrorCode);
    }


    [Fact]
    public async Task ValidationProblemDetails_PreservesFieldErrors()
    {
        var actionContext = CreateActionContext();
        var filters = new List<IFilterMetadata>();
        var original = new BadRequestObjectResult(new ValidationProblemDetails(
            new Dictionary<string, string[]>
            {
                ["Code"] = ["کد تخفیف الزامی است."]
            })
        {
            Detail = "اطلاعات فرم معتبر نیست."
        });

        var context = new ResultExecutingContext(
            actionContext,
            filters,
            original,
            new object());

        var filter = new ResultDtoErrorFilter();
        await filter.OnResultExecutionAsync(
            context,
            () => Task.FromResult(new ResultExecutedContext(
                actionContext,
                filters,
                context.Result,
                new object())));

        var objectResult = Assert.IsType<ObjectResult>(context.Result);
        var result = Assert.IsType<ResultDto>(objectResult.Value);

        Assert.Equal(ResultStatus.ValidationError, result.Status);
        Assert.NotNull(result.Errors);
        Assert.Equal("کد تخفیف الزامی است.", result.Errors!["Code"].Single());
    }

    [Fact]
    public async Task ExistingResultDto_IsNotRewrapped()
    {
        var actionContext = CreateActionContext();
        var filters = new List<IFilterMetadata>();
        var expected = new ResultDto().Conflict(
            "کد محصول تکراری است.",
            "duplicate_product_code");
        var original = new ObjectResult(expected)
        {
            StatusCode = StatusCodes.Status409Conflict
        };

        var context = new ResultExecutingContext(
            actionContext,
            filters,
            original,
            new object());

        var filter = new ResultDtoErrorFilter();
        await filter.OnResultExecutionAsync(
            context,
            () => Task.FromResult(new ResultExecutedContext(
                actionContext,
                filters,
                context.Result,
                new object())));

        var objectResult = Assert.IsType<ObjectResult>(context.Result);
        Assert.Same(expected, objectResult.Value);
    }

    private static ActionContext CreateActionContext()
        => new(
            new DefaultHttpContext(),
            new RouteData(),
            new ActionDescriptor());
}
