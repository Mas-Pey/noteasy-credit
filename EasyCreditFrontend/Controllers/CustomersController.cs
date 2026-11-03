using EasyCreditFrontend.Models;
using EasyCreditFrontend.Models.Dtos;
using EasyCreditFrontend.Models.ViewModels;
using EasyCreditFrontend.Services;
using Microsoft.AspNetCore.Mvc;

namespace EasyCreditFrontend.Controllers;

public class CustomersController : BaseController
{
    private const int DefaultPageSize = 10;

    private readonly ApiClient _apiClient;

    public CustomersController(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? keyword,
        int page = 1,
        int size = DefaultPageSize)
    {
        var model = await BuildIndexAsync(keyword, page, size);
        return View(model);
    }

    // Endpoint MVC yang dipanggil jQuery AJAX.
    // Response berupa potongan HTML (PartialView), bukan JSON.
    [HttpGet]
    public async Task<IActionResult> Rows(
        string? keyword,
        int page = 1,
        int size = DefaultPageSize)
    {
        var model = await BuildIndexAsync(keyword, page, size);
        return PartialView("_CustomerList", model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CustomerFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomerFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var response = await _apiClient.PostAsync<CustomerDto>("/api/customers", new
        {
            name = model.Name,
            nik = model.Nik,
            phone = model.Phone,
            email = model.Email,
            address = model.Address
        });

        if (response is null || !response.IsSuccess || response.Data is null)
        {
            ModelState.AddModelError(
                string.Empty,
                response?.Message ?? "Gagal menyimpan customer.");

            return View(model);
        }

        TempData["Success"] = response.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var response = await _apiClient.GetAsync<CustomerDto>($"/api/customers/{id}");

        if (response?.Data is null)
        {
            TempData["Error"] = response?.Message ?? "Customer tidak ditemukan.";
            return RedirectToAction(nameof(Index));
        }

        return View(ToForm(response.Data));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CustomerFormViewModel model)
    {
        model.CustomerId = id;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var response = await _apiClient.PutAsync<CustomerDto>($"/api/customers/{id}", new
        {
            name = model.Name,
            nik = model.Nik,
            phone = model.Phone,
            email = model.Email,
            address = model.Address
        });

        if (response is null || !response.IsSuccess || response.Data is null)
        {
            ModelState.AddModelError(
                string.Empty,
                response?.Message ?? "Gagal memperbarui customer.");

            return View(model);
        }

        TempData["Success"] = response.Message;
        return RedirectToAction(nameof(Index));
    }

    // Endpoint AJAX: hapus customer tanpa reload halaman.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var response = await _apiClient.DeleteAsync($"/api/customers/{id}");

        return Json(new
        {
            status = response?.IsSuccess ?? false,
            message = response?.Message ?? "Gagal menghapus customer."
        });
    }

    private async Task<CustomerIndexViewModel> BuildIndexAsync(
        string? keyword,
        int page,
        int size)
    {
        page = Math.Max(page, 1);
        size = Math.Clamp(size, 1, 100);

        var url = $"/api/customers?keyword={Uri.EscapeDataString(keyword ?? string.Empty)}" +
                  $"&page={page}&size={size}";

        var response = await _apiClient.GetAsync<PagedData<CustomerDto>>(url);
        var data = response?.Data;

        var total = data?.Total ?? 0;
        var currentPage = data?.Page ?? page;
        var currentSize = data?.Size ?? size;
        var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)currentSize);

        if (response is { IsSuccess: false })
        {
            TempData["Error"] = response.Message;
        }

        return new CustomerIndexViewModel
        {
            Items = data?.Items ?? new List<CustomerDto>(),
            Keyword = keyword,
            Page = currentPage,
            Size = currentSize,
            Total = total,
            TotalPages = totalPages
        };
    }

    private static CustomerFormViewModel ToForm(CustomerDto customer) => new()
    {
        CustomerId = customer.CustomerId,
        Name = customer.Name,
        Nik = customer.Nik,
        Phone = customer.Phone,
        Email = customer.Email,
        Address = customer.Address
    };
}