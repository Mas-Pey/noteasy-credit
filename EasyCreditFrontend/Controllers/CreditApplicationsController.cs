using EasyCreditFrontend.Models;
using EasyCreditFrontend.Models.Dtos;
using EasyCreditFrontend.Models.ViewModels;
using EasyCreditFrontend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EasyCreditFrontend.Controllers;

public class CreditApplicationsController : BaseController
{
    private readonly ApiClient _apiClient;

    public CreditApplicationsController(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return RedirectToAction(nameof(Create));
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new CreateApplicationViewModel();
        await PopulateCustomerOptionsAsync(model);
        return View(model);
    }

    // Endpoint Hitung Simulasi
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

    // Endpoint Submit Loan
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

    private class CustomerListApiData
    {
        public List<CustomerDto>? Items { get; set; }
    }
}
