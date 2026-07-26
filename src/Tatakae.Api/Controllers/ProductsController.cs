using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tatakae.Api.Extensions;
using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Contracts.Products;
using Tatakae.Application.Contracts.Reviews;
using Tatakae.Application.Interfaces.Services;

namespace Tatakae.Api.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(ICatalogService catalog, IProductEngagementService engagement) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] ProductListQuery query, CancellationToken cancellationToken)
    {
        var result = await catalog.GetProductsAsync(query, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("filters")]
    public async Task<IActionResult> GetFilters([FromQuery] ProductListQuery query, CancellationToken cancellationToken)
    {
        var result = await catalog.GetFiltersAsync(query, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("by-slug/{slug}")]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken cancellationToken)
    {
        var result = await catalog.GetBySlugAsync(slug, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("{productId:guid}/reviews")]
    public async Task<IActionResult> GetReviews(Guid productId, CancellationToken cancellationToken)
    {
        var result = await engagement.GetApprovedReviewsAsync(productId, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("{productId:guid}/reviews/summary")]
    public async Task<IActionResult> GetRatingSummary(Guid productId, CancellationToken cancellationToken)
    {
        var result = await engagement.GetRatingSummaryAsync(productId, cancellationToken);
        return result.ToActionResult(this);
    }

    [Authorize]
    [HttpPost("{productId:guid}/reviews")]
    public async Task<IActionResult> SubmitReview(Guid productId, CreateProductReviewRequest request, CancellationToken cancellationToken)
    {
        var mobile = ResolveMobile();
        if (string.IsNullOrWhiteSpace(mobile)) return Unauthorized(new ResultDto().Unauthorized("اطلاعات هویتی کاربر پیدا نشد.", "authenticated_user_required"));
        request.ProductId = productId;

        var result = await engagement.CreateReviewAsync(mobile, request, cancellationToken);
        if (!result.IsSuccess) return result.ToActionResult(this);
        return Created($"/api/products/{productId}/reviews", result.Data);
    }

    [HttpGet("{productId:guid}/questions")]
    public async Task<IActionResult> GetQuestions(Guid productId, CancellationToken cancellationToken)
    {
        var result = await engagement.GetPublicQuestionsAsync(productId, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("{productId:guid}/questions")]
    public async Task<IActionResult> SubmitQuestion(Guid productId, SubmitProductQuestionRequest request, CancellationToken cancellationToken)
    {
        request.ProductId = productId;
        var result = await engagement.SubmitQuestionAsync(request, cancellationToken);
        if (!result.IsSuccess) return result.ToActionResult(this);
        return Created($"/api/products/{productId}/questions", result.Data);
    }

    private string? ResolveMobile() => User.FindFirstValue("mobile") ?? User.FindFirstValue(ClaimTypes.Name);
}
