using System;
using System.Security.Cryptography.X509Certificates;
using EasyCreditApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EasyCreditApi.Infrastructure.Postgres;

public class EfCustomerRepository : ICustomerRepository
{
    private readonly AppDbContext _dbContext;

    public EfCustomerRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> CreateAsync(Customer customer)
    {
        _dbContext.Customers.Add(customer);
        await _dbContext.SaveChangesAsync();
        return customer.CustomerId;
    }

    public async Task<bool> DeleteAsync(int customerId)
    {
        var existing = await _dbContext.Customers
            .FirstOrDefaultAsync(item => item.CustomerId == customerId);

        if (existing == null)
        {
            return false;
        }

        _dbContext.Customers.Remove(existing);
        return await _dbContext.SaveChangesAsync() > 0;
    }

    public async Task<bool> ExistsAsync(int customerId) => await _dbContext.Customers
        .AnyAsync(customer => customer.CustomerId == customerId);

    public async Task<IEnumerable<Customer>> GetAllAsync(string? keyword) => await BuildQuery(keyword)
        .OrderBy(customer => customer.CustomerId)
        .ToListAsync();

    public async Task<Customer?> GetByIdAsync(int customerId) => await _dbContext.Customers
        .AsNoTracking()
        .FirstOrDefaultAsync(customer => customer.CustomerId == customerId);

    public async Task<IEnumerable<Customer>> GetByIdsAsync(IEnumerable<int> customerIds)
    {
        var ids = customerIds.Distinct().ToList();

        if (ids.Count == 0)
        {
            return await Task.FromResult(Enumerable.Empty<Customer>());
        }

        return await _dbContext.Customers
            .AsNoTracking()
            .Where(customer => ids.Contains(customer.CustomerId))
            .ToListAsync();
    }

    public async Task<(IEnumerable<Customer> Items, int Total)> GetPagedAsync(string? keyword, int page, int limit)
    {
        page = Math.Max(page, 1);
        limit = Math.Clamp(limit, 1, 100);

        var query = BuildQuery(keyword);

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(customer => customer.CustomerId)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync();
            
        return (items, total);
    }

    public Task<bool> NikExistsAsync(string nik, int? excludeCustomerId = null) =>
        _dbContext.Customers.AnyAsync(
            customer => customer.Nik == nik &&
            (excludeCustomerId == null || customer.CustomerId != excludeCustomerId)
        );

    public async Task<bool> UpdateAsync(Customer customer)
    {
        var existing = await _dbContext.Customers
            .FirstOrDefaultAsync(item => item.CustomerId == customer.CustomerId);

        if (existing == null)
        {
            return false;
        }

        existing.Name = customer.Name;
        existing.Nik = customer.Nik;
        existing.Email = customer.Email;
        existing.Phone = customer.Phone;
        existing.Address = customer.Address;

        return await _dbContext.SaveChangesAsync() > 0;
    }

    private IQueryable<Customer> BuildQuery(string? keyword)
    {
        var query = _dbContext.Customers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var pattern = $"%{keyword}%";
            query = query.Where(customer => EF.Functions.ILike(customer.Name, pattern) ||
            EF.Functions.ILike(customer.Nik, pattern) ||
            (customer.Email != null && EF.Functions.ILike(customer.Email, pattern)));
        }

        return query;
    }
}
