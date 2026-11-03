using EasyCreditApi.Application.DTO.Credit;
using EasyCreditApi.Domain.Entities;

namespace EasyCreditApi.Infrastructure;

public interface ICreditApplicationSpRepository
{
    Task<IEnumerable<CreditApplication>> GetAllAsync();
    Task<CreditApplication?> GetByIdAsync(int applicationId);
    Task<IEnumerable<ResInstallmentDto>> GetInstallmentsByApplicationIdAsync(int applicationId);
    Task<(string Status, string Message)> ApproveAsync(int applicationId, string approver);
    Task<(string Status, string Message)> RejectAsync(int applicationId, string approver);
    Task<(IEnumerable<ResInstallmentDto> Installments, string Status, string Message)> CalculateInstallmentAsync(decimal loanAmount, int tenorMonths);
    Task<int> CreateWithInstallmentsAsync(
        int customerId,
        decimal loanAmount,
        int tenorMonths,
        decimal interestRate,
        int creditScore,
        IEnumerable<ResInstallmentDto> installments);
}
