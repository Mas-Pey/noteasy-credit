using EasyCreditFrontend.Models;
using EasyCreditFrontend.Models.Dtos;
using EasyCreditFrontend.Models.ViewModels;
using EasyCreditFrontend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EasyCreditFrontend.Controllers;

public class CreditApplicationsController : BaseController
{
    private const int DefaultPageSize = 10;
    private readonly ApiClient _apiClient;

    public CreditApplicationsController(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? status,
        string? keyword,
        int page = 1,
        int size = DefaultPageSize)
    {
        var model = await BuildIndexAsync(status, keyword, page, size);
        return View(model);
    }

    // Endpoint khusus filter / pagination
    [HttpGet]
    public async Task<IActionResult> Rows(
        string? status,
        string? keyword,
        int page = 1,
        int size = DefaultPageSize)
    {
        var model = await BuildIndexAsync(status, keyword, page, size);
        return PartialView("_ApplicationList", model);
    }

    [HttpGet]
    public async Task<IActionResult> Detail(int id)
    {
        var response = await _apiClient.GetAsync<CreditApplicationDetailDto>($"/api/credit-applications/{id}");
        if (response == null || !response.Status || response.Data == null)
        {
            TempData["ErrorMessage"] = response?.Message ?? "Pengajuan kredit tidak ditemukan.";
            return RedirectToAction(nameof(Index));
        }

        return View(response.Data);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new CreateApplicationViewModel();
        await PopulateCustomerOptionsAsync(model);
        return View(model);
    }

    // Endpoint Hitung Simulasi (AJAX)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Preview([FromBody] ReqPreviewInstallmentModel req)
    {
        if (req.LoanAmount < 1000000 || req.LoanAmount > 50000000)
        {
            return BadRequest(new { message = "Nominal pinjaman harus antara Rp 1.000.000 s/d Rp 50.000.000." });
        }

        int[] validTenors = [6, 12, 24, 36];
        if (!validTenors.Contains(req.TenorMonths))
        {
            return BadRequest(new { message = "Tenor harus 6, 12, 24, atau 36 bulan." });
        }

        var apiResponse = await _apiClient.PostAsync<List<InstallmentDto>>(
            "/api/credit-applications/preview-installment",
            new
            {
                loanAmount = req.LoanAmount,
                tenorMonths = req.TenorMonths
            });

        if (apiResponse == null || !apiResponse.Status || apiResponse.Data == null)
        {
            var msg = apiResponse?.Message ?? "Gagal menghitung simulasi cicilan dari API.";
            return BadRequest(new { message = msg });
        }

        return Json(new { status = true, data = apiResponse.Data });
    }

    // Endpoint Submit Pengajuan Kredit Final (AJAX)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] ReqSubmitApplicationModel req)
    {
        if (req.CustomerId <= 0)
        {
            return BadRequest(new { message = "Customer wajib dipilih." });
        }

        if (req.LoanAmount < 1000000 || req.LoanAmount > 50000000)
        {
            return BadRequest(new { message = "Nominal pinjaman harus antara Rp 1.000.000 s/d Rp 50.000.000." });
        }

        int[] validTenors = [6, 12, 24, 36];
        if (!validTenors.Contains(req.TenorMonths))
        {
            return BadRequest(new { message = "Tenor harus 6, 12, 24, atau 36 bulan." });
        }

        var apiResponse = await _apiClient.PostAsync<CreateApplicationResultDto>(
            "/api/credit-applications",
            new
            {
                customerId = req.CustomerId,
                loanAmount = req.LoanAmount,
                tenorMonths = req.TenorMonths
            });

        if (apiResponse == null || !apiResponse.Status || apiResponse.Data == null)
        {
            var msg = apiResponse?.Message ?? "Gagal mengajukan kredit ke API.";
            return BadRequest(new { message = msg });
        }

        return Json(new
        {
            status = true,
            message = apiResponse.Message ?? "Pengajuan kredit berhasil dibuat.",
            data = apiResponse.Data
        });
    }

    private async Task<ApplicationIndexViewModel> BuildIndexAsync(string? status, string? keyword, int page, int size)
    {
        var model = new ApplicationIndexViewModel
        {
            Status = status,
            Keyword = keyword,
            Page = Math.Max(1, page),
            Size = size <= 0 ? DefaultPageSize : size
        };

        var url = $"/api/credit-applications?status={Uri.EscapeDataString(status ?? string.Empty)}&keyword={Uri.EscapeDataString(keyword ?? string.Empty)}&page={model.Page}&size={model.Size}";
        var response = await _apiClient.GetAsync<ApplicationListApiData>(url);

        if (response?.Status == true && response.Data != null)
        {
            model.Total = response.Data.Total;
            model.TotalPages = response.Data.TotalPages;
            model.Items = (response.Data.Items ?? []).Select(dto => new ApplicationRowViewModel
            {
                ApplicationId = dto.ApplicationId,
                CustomerId = dto.CustomerId,
                CustomerName = dto.CustomerName,
                Amount = dto.Amount,
                TenorMonths = dto.TenorMonths,
                Status = dto.Status ?? "PENDING",
                ApprovedBy = dto.ApprovedBy,
                ApprovedAt = dto.ApprovedAt
            }).ToList();
        }

        return model;
    }

    private async Task PopulateCustomerOptionsAsync(CreateApplicationViewModel model)
    {
        try
        {
            var response = await _apiClient.GetAsync<CustomerListApiData>("/api/customers?page=1&size=100");
            if (response?.Status == true && response.Data?.Items != null)
            {
                model.CustomerOptions = response.Data.Items
                    .Select(c => new SelectListItem
                    {
                        Value = c.CustomerId.ToString(),
                        Text = $"{c.Name} - NIK: {c.Nik}"
                    })
                    .ToList();
            }
        }
        catch
        {
            model.CustomerOptions = [];
        }
    }

    public class ReqPreviewInstallmentModel
    {
        public decimal LoanAmount { get; set; }
        public int TenorMonths { get; set; }
    }

    public class ReqSubmitApplicationModel
    {
        public int CustomerId { get; set; }
        public decimal LoanAmount { get; set; }
        public int TenorMonths { get; set; }
    }

    private class ApplicationListApiData
    {
        public int Page { get; set; }
        public int Size { get; set; }
        public int Total { get; set; }
        public int TotalPages { get; set; }
        public List<ApplicationListItemDto>? Items { get; set; }
    }

    private class CustomerListApiData
    {
        public List<CustomerDto>? Items { get; set; }
    }
}
