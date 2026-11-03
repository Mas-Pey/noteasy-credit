using System.Diagnostics;
using EasyCreditFrontend.Models;
using EasyCreditFrontend.Models.Dtos;
using EasyCreditFrontend.Models.ViewModels;
using EasyCreditFrontend.Services;
using Microsoft.AspNetCore.Mvc;

namespace EasyCreditFrontend.Controllers;

public class HomeController : BaseController
{
    private readonly ApiClient _apiClient;

    public HomeController(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var customers = await _apiClient.GetAsync<PagedData<CustomerDto>>(
            "/api/customers?page=1&size=1");

        var applications = await _apiClient.GetAsync<TotalItemsData<ApplicationListItemDto>>(
            "/api/credit-applications");

        var names = await LoadCustomerNamesAsync();

        var items = applications?.Data?.Items ?? new List<ApplicationListItemDto>();
        var rows = items
            .Select(application => ToRow(application, names))
            .OrderByDescending(row => row.ApplicationId)
            .ToList();

        var model = new DashboardViewModel
        {
            ApiAvailable = applications is not null && applications.IsSuccess,
            TotalCustomers = customers?.Data?.Total ?? 0,
            TotalApplications = applications?.Data?.Total ?? rows.Count,
            Pending = rows.Count(row => row.Status is "SUBMITTED" or "PENDING"),
            Approved = rows.Count(row => row.Status == "APPROVED"),
            Rejected = rows.Count(row => row.Status == "REJECTED"),
            Recent = rows.Take(5).ToList()
        };

        if (!model.ApiAvailable)
        {
            TempData["Error"] = applications?.Message
                ?? "Tidak dapat memuat data dari EasyCredit API.";
        }

        return View(model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [HttpGet]
    public IActionResult Error(int? code = null)
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            StatusCode = code ?? 500
        });
    }

    private async Task<Dictionary<int, string>> LoadCustomerNamesAsync()
    {
        var response = await _apiClient.GetAsync<PagedData<CustomerDto>>(
            "/api/customers?page=1&size=100");

        return (response?.Data?.Items ?? new List<CustomerDto>())
            .ToDictionary(customer => customer.CustomerId, customer => customer.Name);
    }

    private static ApplicationRowViewModel ToRow(
        ApplicationListItemDto application,
        IReadOnlyDictionary<int, string> customerNames)
    {
        return new ApplicationRowViewModel
        {
            ApplicationId = application.ApplicationId,
            CustomerId = application.CustomerId,
            CustomerName = customerNames.TryGetValue(application.CustomerId, out var name)
                ? name
                : $"Customer #{application.CustomerId}",
            Amount = application.Amount,
            TenorMonths = application.TenorMonths,
            Status = application.Status ?? "UNKNOWN",
            ApprovedBy = application.AprrovedBy,
            ApprovedAt = application.ApprovedAt
        };
    }
}