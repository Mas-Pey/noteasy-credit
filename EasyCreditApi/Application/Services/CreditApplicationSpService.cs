using EasyCreditApi.Application.DTO.Credit;
using EasyCreditApi.Application.DTO.Customer;
using EasyCreditApi.Application.Interfaces;
using EasyCreditApi.Domain.Entities;
using EasyCreditApi.Infrastructure;
using EasyCreditApi.Middleware;

namespace EasyCreditApi.Application.Services;

public sealed class CreditApplicationSpService : ICreditApplicationSpService
{
    private static readonly int[] ValidTenors = [6, 12, 24, 36];
    private const decimal DefaultInterestRate = 0.01m; // 1% per bulan
    private const int DefaultCreditScore = 700; // Skor internal default

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

    public async Task<(IReadOnlyList<ResCreditApplicationListItemDto> Items, int Total)> GetPagedApplicationsAsync(
        string? status,
        string? keyword,
        int page,
        int size)
    {
        // 1. Ambil seluruh data transaksi kredit dari Oracle
        var apps = (await _repository.GetAllAsync()).ToList();

        // 2. Kumpulkan CustomerId unik
        var customerIds = apps.Select(a => a.CustomerId).Distinct().ToList();

        // 3. Lookup data customer ke PostgreSQL (EF Core) secara terpisah (tanpa JOIN SQL)
        var customers = (await _customerRepository.GetByIdsAsync(customerIds))
            .ToDictionary(c => c.CustomerId);

        // 4. Merge data di memori C#
        var query = apps.Select(app =>
        {
            customers.TryGetValue(app.CustomerId, out var customer);
            return new ResCreditApplicationListItemDto
            {
                ApplicationId = app.ApplicationId,
                CustomerId = app.CustomerId,
                CustomerName = customer?.Name ?? "Unknown Customer",
                CustomerNik = customer?.Nik ?? string.Empty,
                Amount = app.Amount,
                TenorMonths = app.TenorMonths,
                Status = app.Status ?? "PENDING",
                ApplicationDate = DateTime.Now, // Default / fallback
                ApprovedBy = app.ApprovedBy,
                ApprovedAt = app.ApprovedAt
            };
        });

        // 5. Filter status jika ada
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(a => string.Equals(a.Status, status.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        // 6. Filter keyword pencarian nama / NIK
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim();
            query = query.Where(a =>
                a.CustomerName.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                a.CustomerNik.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                a.ApplicationId.ToString() == kw);
        }

        var list = query.OrderByDescending(a => a.ApplicationId).ToList();
        var total = list.Count;

        // 7. Pagination
        var pagedItems = list
            .Skip((Math.Max(1, page) - 1) * Math.Max(1, size))
            .Take(Math.Max(1, size))
            .ToList();

        return (pagedItems, total);
    }

    public async Task<ResCreditApplicationDetailDto> GetDetailAsync(int applicationId)
    {
        // 1. Ambil data header dari Oracle
        var app = await _repository.GetByIdAsync(applicationId);
        if (app is null)
        {
            throw new ExceptionMiddleware.NotFoundException($"Pengajuan kredit dengan ID {applicationId} tidak ditemukan.");
        }

        // 2. Lookup data nasabah dari PostgreSQL (termasuk yang berstatus soft-deleted untuk arsip histori transaksi)
        var customer = await _customerRepository.GetByIdAsync(app.CustomerId, includeDeleted: true);
        ResCustomerDto? customerDto = customer is null ? null : new ResCustomerDto
        {
            CustomerId = customer.CustomerId,
            Name = customer.Name,
            Nik = customer.Nik,
            Phone = customer.Phone,
            Email = customer.Email,
            Address = customer.Address,
            IsDeleted = customer.IsDeleted
        };

        // 3. Ambil seluruh jadwal cicilan dari Oracle
        var installments = (await _repository.GetInstallmentsByApplicationIdAsync(applicationId)).ToList();

        // 4. Rakit detail lengkap
        return new ResCreditApplicationDetailDto
        {
            ApplicationId = app.ApplicationId,
            Amount = app.Amount,
            TenorMonths = app.TenorMonths,
            InterestRate = DefaultInterestRate,
            CreditScore = DefaultCreditScore,
            Status = app.Status ?? "PENDING",
            ApplicationDate = DateTime.Now,
            ApprovedBy = app.ApprovedBy,
            ApprovedAt = app.ApprovedAt,
            Customer = customerDto,
            Installments = installments
        };
    }

    public async Task<string> ApproveAsync(int applicationId, string approver)
    {
        var (status, message) = await _repository.ApproveAsync(applicationId, approver.Trim());

        return status switch
        {
            "SUCCESS" => message,
            "NOT_FOUND" => throw new ExceptionMiddleware.NotFoundException(message),
            "INVALID_STATUS" => throw new ExceptionMiddleware.ValidationException(message),
            _ => throw new InvalidOperationException(message)
        };
    }

    public async Task<string> RejectAsync(int applicationId, string approver)
    {
        var (status, message) = await _repository.RejectAsync(applicationId, approver.Trim());

        return status switch
        {
            "SUCCESS" => message,
            "NOT_FOUND" => throw new ExceptionMiddleware.NotFoundException(message),
            "INVALID_STATUS" => throw new ExceptionMiddleware.ValidationException(message),
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
