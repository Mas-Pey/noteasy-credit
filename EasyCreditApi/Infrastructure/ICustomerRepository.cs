using EasyCreditApi.Domain.Entities;

namespace EasyCreditApi.Infrastructure;

public interface ICustomerRepository
{
    Task<IEnumerable<Customer>> GetAllAsync(string? keyword);
    Task<(IEnumerable<Customer> Items, int Total)> GetPagedAsync(string? keyword, int page, int limit);
    Task<Customer?> GetByIdAsync(int customerId);
    Task<IEnumerable<Customer>> GetByIdsAsync(IEnumerable<int> customerIds);
    Task<bool> ExistsAsync(int customerId);
    Task<bool> NikExistsAsync(string nik, int? excludeCustomerId = null);
    Task<int> CreateAsync(Customer customer);
    Task<bool> UpdateAsync(Customer customer);
    Task<bool> DeleteAsync(int customerId);
}