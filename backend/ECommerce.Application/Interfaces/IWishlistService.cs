namespace ECommerce.Application;

public interface IWishlistService
{
    Task<WishlistResponse> GetWishlistAsync(int userId, bool includeDeleted = false);
    Task<WishlistResponse> AddToWishlistAsync(int userId, int productId);
    Task<WishlistResponse> RemoveFromWishlistAsync(int userId, int productId);
    Task<WishlistResponse> RestoreWishlistItemAsync(int userId, int productId);
    Task<CartResponse> MoveToCartAsync(int userId, int productId);
}
