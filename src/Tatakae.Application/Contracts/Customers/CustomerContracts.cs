namespace Tatakae.Application.Contracts.Customers;

public sealed record CustomerDto(Guid Id, string FullName, string Mobile, string? Email, DateTimeOffset CreatedAt, int OrderCount, decimal LifetimeValue);
