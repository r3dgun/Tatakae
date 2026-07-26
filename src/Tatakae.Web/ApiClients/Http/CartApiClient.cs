using Tatakae.Application.Contracts.Cart;
using Tatakae.Application.Contracts.Common;
using Tatakae.Web.ApiClients.Abstractions;
using Tatakae.Web.ApiClients.Results;
using Tatakae.Web.Models;
using Tatakae.Web.State;

namespace Tatakae.Web.ApiClients.Http;

public sealed class CartApiClient(IApiClientTransport transport, IAuthSessionStore auth) : ICartApiClient
{
    public async Task<ResultDto> MergeAsync(IReadOnlyCollection<CartLine> lines, CancellationToken cancellationToken = default)
    {
        if (lines.Count == 0)
            return new ResultDto().Success("سبد خرید خالی است و نیازی به همگام‌سازی ندارد.");

        await auth.EnsureLoadedAsync();
        if (!auth.IsSignedIn)
            return new ResultDto().Unauthorized("برای همگام‌سازی سبد خرید ابتدا وارد حساب شوید.", "authentication_required");

        var payload = new MergeCartRequest
        {
            Items = lines.Select(line => new AddToCartRequest
            {
                ProductId = line.ProductId,
                VariantId = line.VariantId,
                Quantity = line.Quantity,
                Embroidery = line.Embroidery
            }).ToList()
        };

        return await transport.SendResultAsync(HttpMethod.Post, "api/cart/merge", payload, "همگام‌سازی سبد خرید ناموفق بود.", cancellationToken);
    }

    public async Task<ResultDto> ClearAsync(CancellationToken cancellationToken = default)
    {
        await auth.EnsureLoadedAsync();
        if (!auth.IsSignedIn)
            return new ResultDto().Unauthorized("برای پاک‌کردن سبد خرید ابتدا وارد حساب شوید.", "authentication_required");

        return await transport.SendResultAsync(HttpMethod.Delete, "api/cart", null, "پاک‌کردن سبد خرید ناموفق بود.", cancellationToken);
    }
}
