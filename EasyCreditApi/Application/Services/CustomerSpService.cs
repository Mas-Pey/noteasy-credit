using EasyCreditApi.Application.DTO.Customer;
using EasyCreditApi.Application.Interfaces;
using EasyCreditApi.Domain.Entities;
using EasyCreditApi.Infrastructure;
using EasyCreditApi.Middleware;

namespace EasyCreditApi.Application.Services;

public sealed class CustomerSpService : ICustomerSpService
{
    private readonly ICustomerSpRepository _repository;

    public CustomerSpService(ICustomerSpRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<Customer>> GetAllAsync()
    {
        var items = await _repository.GetAllAsync();
        return items.ToList();
    }

    public Task<Customer?> GetByIdAsync(int customerId) => _repository.GetByIdAsync(customerId);

    public async Task<Customer> CreateAsync(ReqCreateCustomerDto dto)
    {
        var customer = new Customer
        {
            Name = dto.Name.Trim(),
            Nik = dto.Nik.Trim(),
            Phone = dto.Phone,
            Email = dto.Email,
            Address = dto.Address
        };

        var (status, message, id) = await _repository.CreateAsync(customer);

        if (status == "SUCCESS")
            return await _repository.GetByIdAsync(id) ?? customer;

        if (status == "DUPLICATE")
            throw new ExceptionMiddleware.ConflictException(message);

        throw new InvalidOperationException(message);
    }
}