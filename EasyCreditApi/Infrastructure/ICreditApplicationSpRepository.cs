using EasyCreditApi.Application.DTO.Credit;
using EasyCreditApi.Domain.Entities;

namespace EasyCreditApi.Infrastructure;

public interface ICreditApplicationSpRepository
{
    Task<IEnumerable<CreditApplication>> GetAllAsync();
    Task<(string Status, string Message)> ApproveAsync(int applicationId, string approver);
    Task<(IEnumerable<ResInstallmentDto> Installments, string Status, string Message)> CalculateInstallmentAsync(decimal loanAmount, int tenorMonths);
}
