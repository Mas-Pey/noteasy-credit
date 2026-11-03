using EasyCreditApi.Application.DTO.Customer;
using EasyCreditApi.Application.Interfaces;
using EasyCreditApi.Domain.Entities;
using EasyCreditApi.Infrastructure;
using EasyCreditApi.Middleware;
using Oracle.ManagedDataAccess.Client;

namespace EasyCreditApi.Application.Services;

public sealed class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _repository;

    public CustomerService(ICustomerRepository repository) => _repository = repository;

    public async Task<(IReadOnlyList<Customer> Items, int Total)> GetPagedAsync(
        string? keyword, int page, int size)
    {
        var (items, total) = await _repository.GetPagedAsync(keyword, page, size);
        return (items.ToList(), total);
    }

    public Task<Customer?> GetByIdAsync(int customerId) =>
        _repository.GetByIdAsync(customerId);

    public async Task<Customer> CreateAsync(ReqCreateCustomerDto dto)
    {
        var nik = dto.Nik.Trim();

        if (await _repository.NikExistsAsync(nik))
            throw new ExceptionMiddleware.ConflictException("NIK sudah terdaftar.");

        var customer = new Customer
        {
            Name = dto.Name.Trim(),
            Nik = nik,
            Phone = dto.Phone,
            Email = dto.Email,
            Address = dto.Address
        };

        try
        {
            await _repository.CreateAsync(customer);
        }
        catch (OracleException ex) when (ex.Number == 1)
        {
            // Menangkap race condition kalau unique index NIK ditambahkan.
            throw new ExceptionMiddleware.ConflictException("NIK sudah terdaftar.");
        }

        return customer;
    }

    public async Task<Customer?> UpdateAsync(int id, ReqUpdateCustomerDto dto)
    {
        var customer = await _repository.GetByIdAsync(id);
        if (customer is null) return null;

        var nik = dto.Nik.Trim();

        if (await _repository.NikExistsAsync(nik, id))
            throw new ExceptionMiddleware.ConflictException("NIK sudah dipakai customer lain.");

        customer.Name = dto.Name.Trim();
        customer.Nik = nik;
        customer.Phone = dto.Phone;
        customer.Email = dto.Email;
        customer.Address = dto.Address;

        try
        {
            var updated = await _repository.UpdateAsync(customer);
            return updated ? customer : null;
        }
        catch (OracleException ex) when (ex.Number == 1)
        {
            throw new ExceptionMiddleware.ConflictException("NIK sudah dipakai customer lain.");
        }
    }

    public Task<bool> DeleteAsync(int id) => _repository.DeleteAsync(id);
}
