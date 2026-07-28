namespace ECommerce.Application;

public interface ICartService
{
    Task<CartResponse> GetCartAsync(int userId);
    Task<CartResponse> AddToCartAsync(int userId, AddToCartRequest request);
    Task<CartResponse> UpdateCartItemAsync(int userId, int itemId, UpdateCartItemRequest request);
    Task<CartResponse> RemoveFromCartAsync(int userId, int itemId);
    Task<CartResponse> ClearCartAsync(int userId);
}
