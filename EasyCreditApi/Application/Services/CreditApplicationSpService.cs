using EasyCreditApi.Application.DTO.Credit;
using EasyCreditApi.Application.Interfaces;
using EasyCreditApi.Domain.Entities;
using EasyCreditApi.Infrastructure;
using EasyCreditApi.Middleware;

namespace EasyCreditApi.Application.Services;

public sealed class CreditApplicationSpService : ICreditApplicationSpService
{
    private static readonly int[] ValidTenors = [6, 12, 24, 36];
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

    public async Task<IReadOnlyList<ResInstallmentDto>> CalculateInstallmentAsync(ReqPreviewInstallmentDto dto)
    {
        if (dto.LoanAmount < 1_000_000 || dto.LoanAmount > 50_000_000)
        {
            throw new ExceptionMiddleware.ValidationException(
                "Nominal pinjaman harus antara Rp 1.000.000 s/d Rp 50.000.000.");
        }

        if (!ValidTenors.Contains(dto.TenorMonths))
        {
            throw new ExceptionMiddleware.ValidationException(
                "Tenor tidak valid. Pilihan tenor yang diperbolehkan: 6, 12, 24, atau 36 bulan.");
        }

        var (installments, status, message) = await _repository.CalculateInstallmentAsync(
            dto.LoanAmount,
            dto.TenorMonths);

        return status switch
        {
            "SUCCESS" => installments.ToList(),
            "INVALID_AMOUNT" or "INVALID_TENOR" => throw new ExceptionMiddleware.ValidationException(message),
            _ => throw new InvalidOperationException(message)
        };
    }
}
