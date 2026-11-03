using EasyCreditApi.Application.DTO;
using EasyCreditApi.Application.DTO.Auth;
using EasyCreditApi.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EasyCreditApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [AllowAnonymous]  // Anyone can hit this endpoint
    public async Task<IActionResult> Login([FromBody] ReqLoginDto request)
    {
        var result = await _authService.LoginAsync(request.Email, request.Password);
        if (result is null)
            return Unauthorized(ApiResponse<object>.Fail("Invalid email or password"));

        return Ok(ApiResponse<ResLoginDto>.Success(result, "Login successful"));
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] ReqRegisterDto request)
    {
        try
        {
            var user = await _authService.RegisterAsync(request.Email, request.Password, request.Name);
            return Ok(ApiResponse<object>.Success(
                new { user.UserId, user.Email, user.Name },
                "Registration successful"));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<object>.Fail(ex.Message));  // 409 — email already exists
        }
    }
}
