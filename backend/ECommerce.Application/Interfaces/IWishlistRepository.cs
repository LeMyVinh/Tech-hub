using ECommerce.Domain;

namespace ECommerce.Application;

public interface IWishlistRepository
{
    Task<List<WishlistItem>> GetByUserIdAsync(int userId, bool includeDeleted = false);
    Task<WishlistItem?> GetByUserAndProductAsync(int userId, int productId, bool includeDeleted = false);
    Task AddAsync(WishlistItem item);
    Task SoftDeleteAsync(WishlistItem item);
    Task RestoreAsync(WishlistItem item);
    Task SaveChangesAsync();
}
