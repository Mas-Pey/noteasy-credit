using EasyCreditApi.Application.DTO.Credit;
using EasyCreditApi.Application.Interfaces;
using EasyCreditApi.Domain.Entities;
using EasyCreditApi.Infrastructure;
using EasyCreditApi.Middleware;

namespace EasyCreditApi.Application.Services;

public sealed class CreditApplicationSpService : ICreditApplicationSpService
{
    private static readonly int[] ValidTenors = [6, 12, 24, 36];
    private const decimal DefaultInterestRate = 0.01m; // 1% per bulan
    private const int DefaultCreditScore = 700; // Skor internal default sesuai soal

    private readonly ICreditApplicationSpRepository _repository;
    private readonly ICustomerRepository _customerRepository;

    public CreditApplicationSpService(
        ICreditApplicationSpRepository repository,
        ICustomerRepository customerRepository)
    {
        _repository = repository;
        _customerRepository = customerRepository;
    }

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
        ValidateLoanAndTenor(dto.LoanAmount, dto.TenorMonths);

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

    public async Task<ResCreateApplicationDto> CreateAsync(ReqCreateApplicationDto dto)
    {
        // Validasi LoanAmount dan TenorMonths
        ValidateLoanAndTenor(dto.LoanAmount, dto.TenorMonths);

        // Validasi Customer (PostgreSQL)
        var customerExists = await _customerRepository.ExistsAsync(dto.CustomerId);
        if (!customerExists)
        {
            throw new ExceptionMiddleware.ValidationException($"Customer dengan ID {dto.CustomerId} tidak ditemukan.");
        }

        var (installments, status, message) = await _repository.CalculateInstallmentAsync(
            dto.LoanAmount,
            dto.TenorMonths);

        if (status != "SUCCESS")
        {
            throw new ExceptionMiddleware.ValidationException(message);
        }

        var installmentList = installments.ToList();

        // TRN_CREDIT_APPLICATION + TRN_CREDIT_INSTALLMENT
        var newApplicationId = await _repository.CreateWithInstallmentsAsync(
            dto.CustomerId,
            dto.LoanAmount,
            dto.TenorMonths,
            DefaultInterestRate,
            DefaultCreditScore,
            installmentList);

        return new ResCreateApplicationDto
        {
            ApplicationId = newApplicationId,
            CustomerId = dto.CustomerId,
            LoanAmount = dto.LoanAmount,
            TenorMonths = dto.TenorMonths,
            InterestRate = DefaultInterestRate,
            CreditScore = DefaultCreditScore,
            Status = "PENDING",
            ApplicationDate = DateTime.Now,
            TotalInstallments = installmentList.Count
        };
    }

    private static void ValidateLoanAndTenor(decimal loanAmount, int tenorMonths)
    {
        if (loanAmount < 1_000_000 || loanAmount > 50_000_000)
        {
            throw new ExceptionMiddleware.ValidationException(
                "Nominal pinjaman harus antara Rp 1.000.000 s/d Rp 50.000.000.");
        }

        if (!ValidTenors.Contains(tenorMonths))
        {
            throw new ExceptionMiddleware.ValidationException(
                "Tenor tidak valid. Pilihan tenor yang diperbolehkan: 6, 12, 24, atau 36 bulan.");
        }
    }
}
