using EasyCreditApi.Domain.Entities;

namespace EasyCreditApi.Infrastructure;

public interface ICreditApplicationSpRepository
{
    Task<IEnumerable<CreditApplication>> GetAllAsync();
    Task<(string Status, string Message)> ApproveAsync(int applicationId, string approver);
}