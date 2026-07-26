using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Tatakae.Application.Interfaces;
using Tatakae.Domain.Entities;
using Tatakae.Infrastructure.Persistence.Mappers;
using Tatakae.Infrastructure.Persistence.Models;

namespace Tatakae.Infrastructure.Persistence.Repositories;

public sealed partial class SqlCustomerRepository(
    TatakaeDbContext db,
    ILogger<SqlCustomerRepository>? logger = null) : ICustomerRepository
{
    private readonly ILogger<SqlCustomerRepository> _resultLogger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<SqlCustomerRepository>.Instance;

    private async Task<Customer?> GetByMobileAsync(string mobile, CancellationToken cancellationToken = default)
        => (await db.Customers.AsNoTracking().Include(x => x.Addresses).SingleOrDefaultAsync(x => x.Mobile == mobile.Trim(), cancellationToken))?.ToDomain();

    private async Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => (await db.Customers.AsNoTracking().Include(x => x.Addresses).SingleOrDefaultAsync(x => x.Id == id, cancellationToken))?.ToDomain();

    private async Task<IReadOnlyCollection<Customer>> GetAllAsync(CancellationToken cancellationToken = default)
        => (await db.Customers.AsNoTracking().Include(x => x.Addresses).OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken)).Select(x => x.ToDomain()).ToArray();

    private async Task UpsertAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        var incoming = customer.ToRecord();
        var existing = await db.Customers
            .IgnoreQueryFilters()
            .Include(x => x.Addresses)
            .SingleOrDefaultAsync(x => x.Id == customer.Id, cancellationToken);

        if (existing is null)
        {
            db.Customers.Add(incoming);
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        existing.FullName = incoming.FullName;
        existing.Mobile = incoming.Mobile;
        existing.Email = incoming.Email;
        existing.CreatedAt = incoming.CreatedAt;
        db.Restore(existing);

        var incomingAddressIds = incoming.Addresses.Select(x => x.Id).ToHashSet();
        foreach (var storedAddress in existing.Addresses.Where(x => !incomingAddressIds.Contains(x.Id)))
        {
            db.SoftDelete(storedAddress);
        }

        foreach (var incomingAddress in incoming.Addresses)
        {
            var storedAddress = existing.Addresses.SingleOrDefault(x => x.Id == incomingAddress.Id);
            if (storedAddress is null)
            {
                existing.Addresses.Add(incomingAddress);
                continue;
            }

            storedAddress.RecipientName = incomingAddress.RecipientName;
            storedAddress.Mobile = incomingAddress.Mobile;
            storedAddress.Province = incomingAddress.Province;
            storedAddress.City = incomingAddress.City;
            storedAddress.PostalCode = incomingAddress.PostalCode;
            storedAddress.AddressLine = incomingAddress.AddressLine;
            storedAddress.Plaque = incomingAddress.Plaque;
            storedAddress.Unit = incomingAddress.Unit;
            storedAddress.IsDefault = incomingAddress.IsDefault;
            db.Restore(storedAddress);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
    private async Task<IReadOnlyCollection<Address>> GetAddressesAsync(Guid customerId, CancellationToken cancellationToken = default)
        => (await db.CustomerAddresses.AsNoTracking()
                .Where(x => x.CustomerId == customerId)
                .OrderByDescending(x => x.IsDefault)
                .ThenByDescending(x => x.InsertTime)
                .ToListAsync(cancellationToken))
            .Select(x => new Address(x.Id, x.RecipientName, x.Mobile, x.Province, x.City, x.PostalCode, x.AddressLine, x.Plaque, x.Unit, x.IsDefault))
            .ToArray();

    private async Task<Address?> GetAddressAsync(Guid customerId, Guid addressId, CancellationToken cancellationToken = default)
    {
        var row = await db.CustomerAddresses.AsNoTracking().SingleOrDefaultAsync(x => x.CustomerId == customerId && x.Id == addressId, cancellationToken);
        return row is null ? null : new Address(row.Id, row.RecipientName, row.Mobile, row.Province, row.City, row.PostalCode, row.AddressLine, row.Plaque, row.Unit, row.IsDefault);
    }

    private async Task<Address> UpsertAddressAsync(Guid customerId, Address address, CancellationToken cancellationToken = default)
    {
        var customerExists = await db.Customers.AnyAsync(x => x.Id == customerId, cancellationToken);
        if (!customerExists) throw new KeyNotFoundException("Customer not found.");

        var existing = await db.CustomerAddresses
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.CustomerId == customerId && x.Id == address.Id, cancellationToken);
        var shouldBeDefault = address.IsDefault || !await db.CustomerAddresses.AnyAsync(x => x.CustomerId == customerId && x.Id != address.Id, cancellationToken);

        if (shouldBeDefault)
        {
            await db.CustomerAddresses.Where(x => x.CustomerId == customerId && x.Id != address.Id).ExecuteUpdateAsync(setters => setters.SetProperty(x => x.IsDefault, false), cancellationToken);
        }

        if (existing is null)
        {
            existing = new CustomerAddressDbRecord { Id = address.Id == Guid.Empty ? Guid.NewGuid() : address.Id, CustomerId = customerId };
            db.CustomerAddresses.Add(existing);
        }
        else
        {
            db.Restore(existing);
        }

        existing.RecipientName = address.RecipientName.Trim();
        existing.Mobile = address.Mobile.Trim();
        existing.Province = address.Province.Trim();
        existing.City = address.City.Trim();
        existing.PostalCode = address.PostalCode.Trim();
        existing.AddressLine = address.AddressLine.Trim();
        existing.Plaque = address.Plaque?.Trim();
        existing.Unit = address.Unit?.Trim();
        existing.IsDefault = shouldBeDefault;

        await db.SaveChangesAsync(cancellationToken);
        return new Address(existing.Id, existing.RecipientName, existing.Mobile, existing.Province, existing.City, existing.PostalCode, existing.AddressLine, existing.Plaque, existing.Unit, existing.IsDefault);
    }

    private async Task DeleteAddressAsync(Guid customerId, Guid addressId, CancellationToken cancellationToken = default)
    {
        var existing = await db.CustomerAddresses.SingleOrDefaultAsync(x => x.CustomerId == customerId && x.Id == addressId, cancellationToken);
        if (existing is null) throw new KeyNotFoundException("آدرس پیدا نشد.");

        var wasDefault = existing.IsDefault;
        db.SoftDelete(existing);
        await db.SaveChangesAsync(cancellationToken);

        if (wasDefault)
        {
            var next = await db.CustomerAddresses.Where(x => x.CustomerId == customerId).OrderByDescending(x => x.InsertTime).FirstOrDefaultAsync(cancellationToken);
            if (next is not null)
            {
                next.IsDefault = true;
                await db.SaveChangesAsync(cancellationToken);
            }
        }
    }

}
