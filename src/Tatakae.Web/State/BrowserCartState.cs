using System.Text.Json;
using Microsoft.JSInterop;
using Tatakae.Application.Contracts.Account;
using Tatakae.Application.Contracts.Embroidery;
using Tatakae.Application.Contracts.Products;
using Tatakae.Web.Models;

namespace Tatakae.Web.State;

public sealed class BrowserCartState(IJSRuntime js) : ICartState
{
    private const string GuestStorageKey = "tatakae.cart.guest.v2";
    private const string CurrentStorageKey = "tatakae.cart.current.v2";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = false };

    private readonly List<CartLine> _items = [];
    private bool loaded;

    public IReadOnlyList<CartLine> Items => _items;
    public int Count => _items.Sum(x => x.Quantity);
    public decimal Subtotal => _items.Sum(x => x.LineTotal);
    public Guid? OwnerCustomerId { get; private set; }
    public string? OwnerMobile { get; private set; }
    public bool IsAttachedToAccount => OwnerCustomerId.HasValue;
    public event Action? Changed;

    public async Task EnsureLoadedAsync(AccountSessionDto? session = null)
    {
        if (loaded)
        {
            if (session is not null && OwnerCustomerId != session.CustomerId)
            {
                await MergeGuestCartIntoAccountAsync(session);
            }
            return;
        }

        loaded = true;
        if (session is not null)
        {
            await LoadAccountCartAsync(session);
            return;
        }

        var snapshot = await ReadSnapshotAsync(GuestStorageKey) ?? await ReadSnapshotAsync(CurrentStorageKey);
        ReplaceWith(snapshot?.Items ?? [], null, null);
    }

    public async Task MergeGuestCartIntoAccountAsync(AccountSessionDto session)
    {
        loaded = true;

        var accountKey = AccountStorageKey(session.CustomerId);
        var accountSnapshot = await ReadSnapshotAsync(accountKey);
        var guestSnapshot = await ReadSnapshotAsync(GuestStorageKey);

        var merged = new List<CartLine>();
        foreach (var line in accountSnapshot?.Items ?? []) MergeLine(merged, line);
        foreach (var line in guestSnapshot?.Items ?? _items) MergeLine(merged, line);

        ReplaceWith(merged, session.CustomerId, session.Mobile);
        await PersistAsync(writeGuest: false);
        try { await js.InvokeVoidAsync("localStorage.removeItem", GuestStorageKey); } catch { }
    }

    public async Task DetachToGuestAsync()
    {
        OwnerCustomerId = null;
        OwnerMobile = null;
        await PersistAsync(writeGuest: true);
        Changed?.Invoke();
    }

    public void Add(ProductDetailDto product, ProductVariantDto variant, EmbroideryCustomizationRequest embroidery, decimal embroideryPrice)
    {
        // Every custom embroidery configuration is a distinct order line unless product + variant + embroidery are exactly the same.
        MergeLine(_items, CartLine.From(product, variant, embroidery, embroideryPrice));
        NotifyAndPersist();
    }

    public void AddReadyMade(ProductDetailDto product, ProductVariantDto variant)
    {
        // Ready-made embroidered products bypass the studio and carry no extra embroidery price.
        MergeLine(_items, CartLine.FromReadyMade(product, variant));
        NotifyAndPersist();
    }

    public void Increment(string key)
    {
        var item = _items.SingleOrDefault(x => x.Key == key);
        if (item is not null && item.Quantity < 20) item.Quantity++;
        NotifyAndPersist();
    }

    public void Decrement(string key)
    {
        var item = _items.SingleOrDefault(x => x.Key == key);
        if (item is null) return;
        if (item.Quantity > 1) item.Quantity--; else _items.Remove(item);
        NotifyAndPersist();
    }

    public void Remove(string key)
    {
        _items.RemoveAll(x => x.Key == key);
        NotifyAndPersist();
    }

    public void Clear()
    {
        _items.Clear();
        NotifyAndPersist();
    }

    private async Task LoadAccountCartAsync(AccountSessionDto session)
    {
        var snapshot = await ReadSnapshotAsync(AccountStorageKey(session.CustomerId));
        ReplaceWith(snapshot?.Items ?? [], session.CustomerId, session.Mobile);
        await PersistAsync(writeGuest: false);
    }

    private void ReplaceWith(IEnumerable<CartLine> lines, Guid? ownerCustomerId, string? ownerMobile)
    {
        _items.Clear();
        foreach (var line in lines) MergeLine(_items, line);
        OwnerCustomerId = ownerCustomerId;
        OwnerMobile = ownerMobile;
        Changed?.Invoke();
    }

    private void NotifyAndPersist()
    {
        Changed?.Invoke();
        _ = PersistAsync(!OwnerCustomerId.HasValue);
    }

    private async Task PersistAsync(bool writeGuest)
    {
        if (!loaded) return;
        var snapshot = new StoredCartSnapshot
        {
            CustomerId = OwnerCustomerId,
            Mobile = OwnerMobile,
            Items = _items.ToList(),
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var json = JsonSerializer.Serialize(snapshot, JsonOptions);
        try
        {
            await js.InvokeVoidAsync("localStorage.setItem", CurrentStorageKey, json);
            if (OwnerCustomerId.HasValue)
            {
                await js.InvokeVoidAsync("localStorage.setItem", AccountStorageKey(OwnerCustomerId.Value), json);
            }
            else if (writeGuest)
            {
                await js.InvokeVoidAsync("localStorage.setItem", GuestStorageKey, json);
            }
        }
        catch
        {
            // The in-memory cart should remain usable even if storage is unavailable.
        }
    }

    private async Task<StoredCartSnapshot?> ReadSnapshotAsync(string key)
    {
        try
        {
            var json = await js.InvokeAsync<string?>("localStorage.getItem", key);
            if (string.IsNullOrWhiteSpace(json)) return null;
            return JsonSerializer.Deserialize<StoredCartSnapshot>(json, JsonOptions);
        }
        catch { return null; }
    }

    private static string AccountStorageKey(Guid customerId) => $"tatakae.cart.account.{customerId:N}.v2";

    private static void MergeLine(List<CartLine> target, CartLine source)
    {
        var existing = target.FirstOrDefault(x => SameConfiguration(x, source));
        if (existing is null)
        {
            target.Add(source);
            return;
        }

        existing.Quantity = Math.Clamp(existing.Quantity + source.Quantity, 1, 20);
    }

    private static bool SameConfiguration(CartLine left, CartLine right)
    {
        if (left.ProductId != right.ProductId || left.VariantId != right.VariantId || left.SupportsEmbroidery != right.SupportsEmbroidery) return false;
        if (!left.SupportsEmbroidery && !right.SupportsEmbroidery) return true;
        return EmbroiderySignature(left.Embroidery) == EmbroiderySignature(right.Embroidery);
    }

    private static string EmbroiderySignature(EmbroideryCustomizationRequest embroidery)
        => JsonSerializer.Serialize(new
        {
            embroidery.Placement,
            embroidery.WidthCm,
            embroidery.HeightCm,
            embroidery.ThreadColorCount,
            ThreadColorHexes = embroidery.ThreadColorHexes.OrderBy(x => x).ToArray(),
            embroidery.DesignSource,
            embroidery.MotifKey,
            embroidery.ArtworkFileUrl,
            embroidery.ArtworkFileName,
            embroidery.Text,
            embroidery.FontName,
            embroidery.PositionX,
            embroidery.PositionY,
            embroidery.ScalePercent,
            embroidery.RotationDegrees,
            embroidery.OpacityPercent,
            embroidery.Note
        }, JsonOptions);

    public sealed class StoredCartSnapshot
    {
        public Guid? CustomerId { get; set; }
        public string? Mobile { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public List<CartLine> Items { get; set; } = [];
    }
}
