using ECommerce.Domain;

namespace ECommerce.Application;

public interface IWishlistRepository
{
    Task<List<WishlistItem>> GetByUserIdAsync(int userId);
    Task<WishlistItem?> GetByUserAndProductAsync(int userId, int productId);
    Task AddAsync(WishlistItem item);
    Task RemoveAsync(WishlistItem item);
    Task SaveChangesAsync();
}