using EasyCreditFrontend.Models.Dtos;
using EasyCreditFrontend.Models.ViewModels;
using EasyCreditFrontend.Services;
using Microsoft.AspNetCore.Mvc;

namespace EasyCreditFrontend.Controllers;

public class AuthController : Controller
{
    private readonly ApiClient _apiClient;

    public AuthController(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (!string.IsNullOrEmpty(HttpContext.Session.GetString(SessionKeys.Token)))
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var response = await _apiClient.PostAsync<LoginDataDto>("/api/auth/login", new
        {
            email = model.Email,
            password = model.Password
        });

        if (response is null || !response.IsSuccess || response.Data is null)
        {
            ModelState.AddModelError(
                string.Empty,
                response?.Message ?? "Login gagal. Periksa email dan password.");

            return View(model);
        }

        HttpContext.Session.SetString(SessionKeys.Token, response.Data.AccessToken);
        HttpContext.Session.SetString(SessionKeys.Role, response.Data.Role);
        HttpContext.Session.SetString(SessionKeys.Name, response.Data.Name);

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Login));
    }
}
