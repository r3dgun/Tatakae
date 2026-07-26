using Tatakae.Application.Contracts.Common;
using Tatakae.Domain.Entities;

namespace Tatakae.Application.Interfaces;

public interface ICustomerRepository
{
    Task<ResultDto<Customer>> GetByMobileAsync(string mobile, CancellationToken cancellationToken = default);
    Task<ResultDto<Customer>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ResultDto<IReadOnlyCollection<Customer>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ResultDto<Customer>> UpsertAsync(Customer customer, CancellationToken cancellationToken = default);
    Task<ResultDto<IReadOnlyCollection<Address>>> GetAddressesAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<ResultDto<Address>> GetAddressAsync(Guid customerId, Guid addressId, CancellationToken cancellationToken = default);
    Task<ResultDto<Address>> UpsertAddressAsync(Guid customerId, Address address, CancellationToken cancellationToken = default);
    Task<ResultDto> DeleteAddressAsync(Guid customerId, Guid addressId, CancellationToken cancellationToken = default);
}
