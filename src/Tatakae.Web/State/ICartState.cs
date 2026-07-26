using Tatakae.Application.Contracts.Account;
using Tatakae.Application.Contracts.Embroidery;
using Tatakae.Application.Contracts.Products;
using Tatakae.Web.Models;

namespace Tatakae.Web.State;

public interface ICartState
{
    IReadOnlyList<CartLine> Items { get; }
    int Count { get; }
    decimal Subtotal { get; }
    Guid? OwnerCustomerId { get; }
    string? OwnerMobile { get; }
    bool IsAttachedToAccount { get; }
    event Action? Changed;
    Task EnsureLoadedAsync(AccountSessionDto? session = null);
    Task MergeGuestCartIntoAccountAsync(AccountSessionDto session);
    Task DetachToGuestAsync();
    void Add(ProductDetailDto product, ProductVariantDto variant, EmbroideryCustomizationRequest embroidery, decimal embroideryPrice);
    void AddReadyMade(ProductDetailDto product, ProductVariantDto variant);
    void Increment(string key);
    void Decrement(string key);
    void Remove(string key);
    void Clear();
}
