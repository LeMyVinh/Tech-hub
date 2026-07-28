using ECommerce.Domain;

namespace ECommerce.Application;

public interface ICartRepository
{
    Task<Cart?> GetByUserIdAsync(int userId);
    Task AddAsync(Cart cart);
    Task SaveChangesAsync();
}
