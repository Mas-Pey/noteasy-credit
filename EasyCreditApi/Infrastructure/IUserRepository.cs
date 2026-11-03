using EasyCreditApi.Domain.Entities;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    // Task<User?> GetByIdAsync(int userId);
    Task AddAsync(User user);
}
