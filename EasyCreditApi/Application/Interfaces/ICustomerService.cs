using EasyCreditApi.Application.DTO.Customer;
using EasyCreditApi.Domain.Entities;

namespace EasyCreditApi.Application.Interfaces;

public interface ICustomerService
{
    Task<(IReadOnlyList<Customer> Items, int Total)> GetPagedAsync(
        string? keyword, int page, int size);
    Task<Customer?> GetByIdAsync(int customerId);
    Task<Customer> CreateAsync(ReqCreateCustomerDto dto);
    Task<Customer?> UpdateAsync(int id, ReqUpdateCustomerDto dto);
    Task<bool> DeleteAsync(int id);
}