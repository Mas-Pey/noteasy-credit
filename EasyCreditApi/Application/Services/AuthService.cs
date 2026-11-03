using EasyCreditApi.Application.DTO.Auth;
using EasyCreditApi.Application.Interfaces;
using EasyCreditApi.Domain.Entities;

namespace EasyCreditApi.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<ResLoginDto?> LoginAsync(string email, string password)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user is null || !_passwordHasher.Verify(password, user.PasswordHash))
        {
            return null;
        }

        var (token, expiresAt) = _jwtTokenGenerator.GenerateToken(user.UserId, user.Name, user.Role);
        return new ResLoginDto(token, user.Role, user.Name, expiresAt);
    }

    public async Task<User> RegisterAsync(string email, string password, string name)
    {
        var existing = await _userRepository.GetByEmailAsync(email);
        if (existing is not null)
            throw new InvalidOperationException("Email already registered");

        var user = new User
        {
            Email = email,
            PasswordHash = _passwordHasher.Hash(password),
            Name = name,
            Role = "User",
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user);
        return user;
    }
}
