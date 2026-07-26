# Phase 26 — Inventory reservation cleanup with Hangfire

## Goal

When checkout succeeds, inventory is reserved for a limited time while the order remains visible in the customer's account. A recurring Hangfire job releases abandoned reservations after their deadline.

## Source of truth

`InventoryReservations.ExpiresAt` is the source of truth. Hangfire is only the cleanup/recovery mechanism. Payment start, payment verification, manual cancellation, and cleanup all use conditional reservation states so repeated execution is safe.

## Flow

1. Checkout opens a serializable SQL transaction.
2. Available inventory is checked as `StockQuantity - ReservedQuantity`.
3. `ReservedQuantity` is incremented and `InventoryReservations` rows are inserted with status `Reserved`.
4. The pending order and its first status-history row are inserted in the same transaction.
5. The account order card shows the reservation deadline and a **Complete payment** action while the reservation is active.
6. Starting payment validates the reservation and extends it only for the configured gateway grace window, capped by `MaximumLifetimeMinutes`.
7. Successful payment atomically changes reservation state to `Consumed`, decrements both `ReservedQuantity` and `StockQuantity`, and marks the order/payment paid.
8. Cancellation or timeout changes reservation state to `Released`/`Expired` and decrements only `ReservedQuantity`.
9. The recurring job scans expired `Reserved` rows every minute in bounded batches and cancels still-unpaid orders.

## Configuration

```json
"InventoryReservations": {
  "HoldMinutes": 15,
  "PaymentGraceMinutes": 10,
  "MaximumLifetimeMinutes": 30,
  "CleanupBatchSize": 200,
  "MaxBatchesPerRun": 10,
  "CleanupCron": "* * * * *"
}
```

- `HoldMinutes`: initial checkout reservation period.
- `PaymentGraceMinutes`: minimum gateway round-trip window when payment starts.
- `MaximumLifetimeMinutes`: hard cap measured from reservation creation.
- `CleanupBatchSize`: maximum reservation orders loaded per cleanup batch.
- `MaxBatchesPerRun`: upper bound for one recurring execution.
- `CleanupCron`: Hangfire CRON expression.

## Hangfire registration

The API registers a server that listens to the `inventory` and `default` queues. The recurring job ID is stable:

```csharp
var recurringJobs = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
recurringJobs.AddOrUpdate<InventoryReservationCleanupJob>(
    "inventory-reservations-expire",
    "inventory",
    job => job.RunAsync(CancellationToken.None),
    reservationOptions.CleanupCron,
    new RecurringJobOptions
    {
        TimeZone = TimeZoneInfo.Utc
    });
```

No public Hangfire dashboard endpoint is enabled by this phase.

## Operational notes

- Keep at least one Hangfire Server process running.
- The cleanup job uses a distributed non-overlap lock and serializable database transactions.
- Job retries and repeated executions are safe because only `Reserved` rows are released.
- SQL Server's existing `IX_InventoryReservations_Status_ExpiresAt` index supports the expiration scan.
- Existing orders created before this phase do not automatically gain reservation rows.

## Phase 26.3 payment finalization compatibility fix

Payment initialization and ordinary payment persistence continue to use the
Phase 25 `EfPaymentRepository` path. Successful payment finalization in the
running application is routed through `IPaidOrderInventoryFinalizer`, whose EF
implementation persists the payment/order Paid transition and consumes the
reservation inside one serializable SQL transaction.

This separation has two benefits:

- isolated/legacy payment tests and orders without reservation rows keep their
  previous behavior;
- production checkout orders still receive atomic payment and inventory
  finalization.

The recurring inventory job also reconciles any Paid order that still has
`Reserved` rows before processing expired unpaid reservations. Therefore a Paid
order is never released by the expiration cleanup path.

## Phase 26.4 payment isolation fix

`PaymentService` and `EfPaymentRepository` are restored byte-for-byte to their
Phase 25 implementations. Reservation behavior is now composed only in the
production dependency-injection graph through `ReservationAwarePaymentRepository`.

The decorator:

- validates and extends an existing active reservation before creating a payment;
- allows legacy orders that have no reservation rows;
- routes only successful Paid transitions to `IPaidOrderInventoryFinalizer`;
- delegates all reads, unsuccessful outcomes, refunds, and admin listing to the
  baseline repository.

This keeps `PaymentServiceTests` independent from Hangfire and inventory while
preserving atomic paid-order inventory finalization in the running API.
