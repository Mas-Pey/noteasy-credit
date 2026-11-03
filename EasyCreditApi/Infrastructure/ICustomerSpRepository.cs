using EasyCreditApi.Domain.Entities;

namespace EasyCreditApi.Infrastructure;

public interface ICustomerSpRepository
{
    Task<IEnumerable<Customer>> GetAllAsync();
    Task<Customer?> GetByIdAsync(int customerId);
    Task<(string Status, string Message, int CustomerId)> CreateAsync(Customer customer);
}