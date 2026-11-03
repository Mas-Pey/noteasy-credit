using EasyCreditApi.Application.Interfaces;
using EasyCreditApi.Domain.Entities;

namespace EasyCreditApi.Infrastructure;

public class InMemoryUserRepository : IUserRepository
{
    private readonly List<User> _users = new();

    public InMemoryUserRepository(IPasswordHasher hasher)
    {
        _users.Add(new User
        {
            UserId = 1,
            Name = "Admin",
            Email = "admin@mail.com",
            Role = "Admin",
            PasswordHash = hasher.Hash("Admin123!"),
            CreatedAt = DateTime.Now
        });
    }

    public Task<User?> GetByEmailAsync(string email) =>
        Task.FromResult(_users.FirstOrDefault( u =>
            u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)));

    public Task AddAsync(User user)
    {
        user.UserId = _users.Count + 1;
        _users.Add(user);
        return Task.CompletedTask;
    }
}
