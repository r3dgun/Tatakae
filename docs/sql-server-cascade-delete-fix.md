# SQL Server cascade delete fix

SQL Server rejects schemas that create multiple cascade delete paths, for example:

`FK_CartItems_Products_ProductId may cause cycles or multiple cascade paths`

This ecommerce project uses soft delete through `BaseEntity`:

- `IsRemoved`
- `RemoveTime`
- `InsertTime`
- `UpdateTime`

Because of that, physical cascade delete is disabled globally in `TatakaeDbContext` by setting every foreign key to `DeleteBehavior.NoAction` at the end of `OnModelCreating`.

This is intentional for a production ecommerce database. Orders, cart items, products, permissions, invoices, shipments, reviews and audit records should not be physically deleted automatically.

If you need to remove a record, mark it as removed instead of deleting it from the database.

```csharp
entity.IsRemoved = true;
entity.RemoveTime = DateTime.Now;
```

Database name for this fixed version:

`TatakaeEmbroideryCommerce_FinalReviewedFixed4V1`
