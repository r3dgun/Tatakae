# Phase 18 hotfix: soft-delete seed product with required relationships

The seed restore regression test must soft-delete the product through `TatakaeDbContext.SoftDelete` rather than physically removing the aggregate.

All EF Core foreign keys are configured with `DeleteBehavior.ClientNoAction`. This prevents EF Core from severing required relationships in memory before `SaveChanges` converts an accidental `Deleted` state into a soft delete. The database still receives `NO ACTION`, so physical deletion remains protected by foreign-key constraints.

The affected regression test verifies that the fixed seed product is restored without inserting a duplicate primary key.
