using EasyCreditApi.Application.Interfaces;
using EasyCreditApi.Domain.Entities;
using EasyCreditApi.Infrastructure;
using EasyCreditApi.Middleware;

namespace EasyCreditApi.Application.Services;

public sealed class CreditApplicationSpService : ICreditApplicationSpService
{
    private readonly ICreditApplicationSpRepository _repository;

    public CreditApplicationSpService(ICreditApplicationSpRepository repository)
        => _repository = repository;

    public async Task<IReadOnlyList<CreditApplication>> GetAllAsync()
    {
        var items = await _repository.GetAllAsync();
        return items.ToList();
    }

    public async Task<string> ApproveAsync(int applicationId, string approver)
    {
        var (status, message) = await _repository.ApproveAsync(applicationId, approver.Trim());

        return status switch
        {
            "SUCCESS" => message,
            "NOT_FOUND" => throw new ExceptionMiddleware.NotFoundException(message),
            "INVALID_STATUS" => throw new ExceptionMiddleware.ConflictException(message),
            _ => throw new InvalidOperationException(message)
        };
    }
}