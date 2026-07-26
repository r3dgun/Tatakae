# ResultDto repository/service pattern — Coupon vertical slice

The coupon flow now uses one result contract from persistence to UI:

- `ICouponRepository` returns `ResultDto` / `ResultDto<T>` for every method.
- `SqlCouponRepository` and `InMemoryCouponRepository` validate input, catch persistence errors, log them, and return Persian messages.
- `IAdminCouponService` exposes Result-based CRUD plus `GetByIdAsync`.
- `AdminCouponService` consumes repository results directly; it does not throw for validation, duplicate code, not-found, or persistence failure.
- `ICouponService` and `CouponService` return `ResultDto<CouponQuoteDto>`.
- API controllers inject interfaces and return the complete ResultDto response.
- Web clients unwrap `Data` only after checking both the HTTP status and `IsSuccess`.

Use the same convention for another repository/service pair:

```csharp
public interface IEntityRepository
{
    Task<ResultDto<IReadOnlyCollection<Entity>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ResultDto<Entity>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ResultDto<Entity>> UpsertAsync(Entity entity, CancellationToken cancellationToken = default);
    Task<ResultDto> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
```

A service method should:

1. Create the appropriate `ResultDto`.
2. Validate request/id.
3. Await the repository result.
4. Stop and propagate `repositoryResult.Message` when it failed.
5. Map domain data to DTO.
6. Catch unexpected exceptions, log structured identifiers, and return a safe Persian message.
