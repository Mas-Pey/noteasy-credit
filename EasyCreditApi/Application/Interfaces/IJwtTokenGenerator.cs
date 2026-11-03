using EasyCreditApi.Domain.Entities;

namespace EasyCreditApi.Application.Interfaces;
public interface IJwtTokenGenerator
{
    (string Token, DateTime ExpiresAt) GenerateToken(
        int userId,
        string name,
        string role);
}
