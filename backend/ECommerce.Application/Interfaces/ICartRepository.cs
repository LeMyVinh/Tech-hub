using ECommerce.Domain;

namespace ECommerce.Application;

public interface ICartRepository
{
    Task<Cart?> GetByUserIdAsync(int userId);
    Task AddAsync(Cart cart);
    Task SaveChangesAsync();
    Task<Cart> EnsureCartAsync(int userId);
    Task AddOrIncrementItemAsync(int cartId, int variantId, int quantity);
}