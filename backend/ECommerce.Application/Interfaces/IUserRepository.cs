using ECommerce.Domain;

namespace ECommerce.Application;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(int id);
    Task<List<User>> GetAllAsync(int page, int pageSize);
    Task<int> GetCountAsync();
    Task AddAsync(User user);
    Task SaveChangesAsync();
}
