using EasyCreditApi.Application.DTO.Credit;
using EasyCreditApi.Domain.Entities;

namespace EasyCreditApi.Application.Interfaces;

public interface ICreditApplicationSpService
{
    Task<IReadOnlyList<CreditApplication>> GetAllAsync();
    Task<(IReadOnlyList<ResCreditApplicationListItemDto> Items, int Total)> GetPagedApplicationsAsync(
        string? status,
        string? keyword,
        int page,
        int size);
    Task<ResCreditApplicationDetailDto> GetDetailAsync(int applicationId);
    Task<string> ApproveAsync(int applicationId, string approver);
    Task<IReadOnlyList<ResInstallmentDto>> CalculateInstallmentAsync(ReqPreviewInstallmentDto dto);
    Task<ResCreateApplicationDto> CreateAsync(ReqCreateApplicationDto dto);
}
