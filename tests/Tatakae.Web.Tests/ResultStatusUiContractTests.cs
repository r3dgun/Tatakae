using System.Net;
using System.Net.Http.Json;
using Tatakae.Application.Contracts.Common;
using Tatakae.Web.ApiClients.Results;

namespace Tatakae.Web.Tests;

public sealed class ResultStatusUiContractTests
{
    [Fact]
    public async Task Api_reader_maps_http_404_to_not_found_result()
    {
        var expected = new ResultDto<string>().NotFound("محصول پیدا نشد.", "product_not_found");
        using var response = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = JsonContent.Create(expected)
        };

        var reader = new ApiResultReader();
        var result = await reader.ReadAsync<string>(response, "خطای دریافت محصول.");

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Equal("product_not_found", result.ErrorCode);
        Assert.Equal("محصول پیدا نشد.", result.Message);
    }

    [Fact]
    public void Global_error_boundary_uses_result_status_view()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "ArchitectureFixtures");
        var app = File.ReadAllText(Path.Combine(root, "App.razor"));

        Assert.Contains("<ResultStatusView Error=\"@error\" />", app);
        Assert.Contains("Status=\"ResultStatus.NotFound\"", app);
        Assert.DoesNotContain("ارتباط با سرویس برقرار نشد", app);
    }

    [Fact]
    public void Status_view_has_dedicated_views_for_supported_result_statuses()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "ArchitectureFixtures");
        var view = File.ReadAllText(Path.Combine(root, "Shared", "ResultStatusView.razor"));

        Assert.Contains("ResultStatus.Unauthorized => 401", view);
        Assert.Contains("ResultStatus.Forbidden => 403", view);
        Assert.Contains("ResultStatus.NotFound => 404", view);
        Assert.Contains("ResultStatus.Conflict => 409", view);
        Assert.Contains("خطاهای اعتبارسنجی", view);
        Assert.Contains("apiError.Result", view);
    }
}
