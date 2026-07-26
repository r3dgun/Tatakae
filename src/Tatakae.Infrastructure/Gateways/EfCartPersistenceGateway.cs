using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tatakae.Application.Contracts.Cart;
using Tatakae.Application.Interfaces.Gateways;
using Tatakae.Infrastructure.Persistence;
using Tatakae.Infrastructure.Persistence.Models;

namespace Tatakae.Infrastructure.Gateways;

public sealed class EfCartPersistenceGateway(TatakaeDbContext db) : ICartPersistenceGateway
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<CartMergeResultDto> MergeAsync(
        MergeCartRequest request,
        CartCustomerContext customer,
        CancellationToken cancellationToken = default)
        => MergeCoreAsync(request, customer, cancellationToken);

    public Task ClearAsync(
        CartCustomerContext customer,
        CancellationToken cancellationToken = default)
        => ClearCoreAsync(customer, cancellationToken);

    private async Task<CartMergeResultDto> MergeCoreAsync(
        MergeCartRequest request,
        CartCustomerContext customerContext,
        CancellationToken cancellationToken)
    {
        var customerId = await ResolveCustomerIdAsync(customerContext, cancellationToken)
            ?? throw new InvalidOperationException("برای اتصال سبد خرید به حساب، پروفایل مشتری پیدا نشد.");

        var now = DateTimeOffset.UtcNow;
        var cart = await db.Carts
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.CustomerId == customerId, cancellationToken);

        if (cart is null)
        {
            cart = new CartDbRecord
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                CreatedAt = now,
                UpdatedAt = now
            };
            db.Carts.Add(cart);
        }

        foreach (var item in request.Items.Where(x => x.Quantity > 0))
        {
            var embroideryJson = JsonSerializer.Serialize(item.Embroidery, JsonOptions);
            var existing = cart.Items.FirstOrDefault(x =>
                x.ProductId == item.ProductId &&
                x.ProductVariantId == item.VariantId &&
                string.Equals(x.EmbroideryConfigurationJson, embroideryJson, StringComparison.Ordinal));

            if (existing is null)
            {
                cart.Items.Add(new CartItemDbRecord
                {
                    Id = Guid.NewGuid(),
                    CartId = cart.Id,
                    ProductId = item.ProductId,
                    ProductVariantId = item.VariantId,
                    Quantity = Math.Clamp(item.Quantity, 1, 20),
                    EmbroideryConfigurationJson = embroideryJson,
                    CreatedAt = now
                });
            }
            else
            {
                existing.Quantity = Math.Clamp(existing.Quantity + item.Quantity, 1, 20);
            }
        }

        cart.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);
        return new CartMergeResultDto(cart.Items.Sum(x => x.Quantity), cart.UpdatedAt);
    }

    private async Task ClearCoreAsync(
        CartCustomerContext customerContext,
        CancellationToken cancellationToken)
    {
        var customerId = await ResolveCustomerIdAsync(customerContext, cancellationToken);
        if (customerId is null) return;

        var cart = await db.Carts
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.CustomerId == customerId.Value, cancellationToken);

        if (cart is null) return;

        db.SoftDeleteRange(cart.Items);
        cart.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Guid?> ResolveCustomerIdAsync(
        CartCustomerContext context,
        CancellationToken cancellationToken)
    {
        ApplicationUserIdentity? user = null;
        if (context.IdentityUserId.HasValue)
        {
            user = await db.Users.FirstOrDefaultAsync(x => x.Id == context.IdentityUserId.Value, cancellationToken);
            if (user?.CustomerId is not null) return user.CustomerId.Value;
        }

        if (string.IsNullOrWhiteSpace(context.Mobile)) return null;

        var customer = await db.Customers.FirstOrDefaultAsync(x => x.Mobile == context.Mobile, cancellationToken);
        if (customer is null)
        {
            customer = new CustomerDbRecord
            {
                Id = Guid.NewGuid(),
                FullName = context.FullName,
                Mobile = context.Mobile,
                Email = context.Email,
                CreatedAt = DateTimeOffset.UtcNow
            };
            db.Customers.Add(customer);
        }
        else
        {
            customer.FullName = context.FullName;
            customer.Email = context.Email;
        }

        if (user is not null) user.CustomerId = customer.Id;
        await db.SaveChangesAsync(cancellationToken);
        return customer.Id;
    }
}
