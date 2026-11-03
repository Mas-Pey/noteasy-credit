using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EasyCreditApi.Application.Interfaces;
using EasyCreditApi.Domain.Entities;
using Microsoft.IdentityModel.Tokens;

namespace EasyCreditApi.Infrastructure.Security;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtSettings _settings;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        _settings = configuration.GetSection("Jwt").Get<JwtSettings>()
            ?? throw new InvalidOperationException("Konfigurasi 'Jwt' tidak ditemukan");
    }

    public (string Token, DateTime ExpiresAt) GenerateToken(int userID, string name, string role)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes);

        var claims = new[]
        {
           new Claim(ClaimTypes.NameIdentifier, userID.ToString()),
           new Claim(ClaimTypes.Name, name),
           new Claim(ClaimTypes.Role, role),
           new Claim("iat", DateTimeOffset.Now.ToUnixTimeSeconds().ToString())
        };

        var credentials = new SigningCredentials(
            _settings.GetSigningKey(),
            SecurityAlgorithms.HmacSha256
        );

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}

public class JwtSettings
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 60;

    public SymmetricSecurityKey GetSigningKey() =>
        new(Encoding.UTF8.GetBytes(Key));
}
