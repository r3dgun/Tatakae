# Phase 17 - Soft Delete and Global Database Filters

## Behavior

All EF Core persistence records that implement `IBaseEntity` use the same audit fields:

- `IsRemoved`
- `RemoveTime` (`RemovedAt` is a non-mapped code alias)
- `InsertTime`
- `UpdateTime`

User-facing delete operations mark `IsRemoved = true`, set `RemoveTime`, and keep the row in the database. Normal EF Core queries exclude removed rows automatically through a global query filter. `TatakaeDbContext.SaveChanges` also converts an accidental EF `Deleted` state for any `IBaseEntity` into a soft delete, preventing future repository code from physically deleting these records by mistake.

Use `IgnoreQueryFilters()` only in restore, audit, data-repair, or administrative tooling:

```csharp
var removedCoupon = await db.Coupons
    .IgnoreQueryFilters()
    .SingleOrDefaultAsync(x => x.Id == id && x.IsRemoved, cancellationToken);
```

## Supported delete paths

Soft delete is applied to:

- products and their catalog/engagement dependent rows
- categories
- coupons
- customer addresses
- shipping methods
- media assets
- wishlist entries

Adding a previously removed wishlist row restores the existing row instead of creating a duplicate. Category, coupon, customer, address, and shipping upserts can also restore a matching removed row when addressed by its identifier.

## Unique indexes

Unique business indexes on soft-deletable records are configured as SQL Server filtered indexes:

```sql
WHERE [IsRemoved] = 0
```

This allows a new active slug, SKU, coupon code, mobile, or other business key to reuse a value owned only by a removed row.

## Existing databases

The model already contained `IsRemoved` and `RemoveTime`, so no new columns are required. Existing databases still need their unique indexes rebuilt with the new filter. The project currently uses `EnsureCreated`; before production deployment, introduce EF Core migrations and generate a migration for the filtered-index model changes.

## Tests

`SoftDeletePersistenceTests` verifies:

- every `IBaseEntity` EF model has a query filter
- unique business indexes are filtered by `IsRemoved`
- coupon deletion preserves the row and hides it from default queries
- product deletion marks product images and variants with the same removal timestamp
- restoring a wishlist entry does not create a duplicate row
