using EasyCreditApi.Domain.Entities;

namespace EasyCreditApi.Application.Interfaces;

public interface ICreditApplicationSpService
{
    Task<IReadOnlyList<CreditApplication>> GetAllAsync();
    Task<string> ApproveAsync(int applicationId, string approver);
}