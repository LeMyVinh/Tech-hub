namespace ECommerce.Application;

public interface IWishlistService
{
    Task<WishlistResponse> GetWishlistAsync(int userId);
    Task<WishlistResponse> AddToWishlistAsync(int userId, int productId);
    Task<WishlistResponse> RemoveFromWishlistAsync(int userId, int productId);
    Task<CartResponse> MoveToCartAsync(int userId, int productId);
}
