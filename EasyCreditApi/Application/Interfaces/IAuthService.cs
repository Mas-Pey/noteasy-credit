using EasyCreditApi.Application.DTO.Auth;
using EasyCreditApi.Domain.Entities;

namespace EasyCreditApi.Application.Interfaces;
public interface IAuthService
{
    Task<ResLoginDto?> LoginAsync(string email, string password);
    Task<User> RegisterAsync(string email, string password, string name);
}
