using ECommerce.Domain;

namespace ECommerce.Application;

public class WishlistService : IWishlistService
{
    private readonly IWishlistRepository _wishlistRepository;
    private readonly ICartService _cartService;

    public WishlistService(IWishlistRepository wishlistRepository, ICartService cartService)
    {
        _wishlistRepository = wishlistRepository;
        _cartService = cartService;
    }

    public async Task<WishlistResponse> GetWishlistAsync(int userId)
    {
        var items = await _wishlistRepository.GetByUserIdAsync(userId);
        var responseItems = items.Select(i => new WishlistItemResponse(
            i.Id,
            i.ProductId,
            i.Product.Name,
            i.Product.ProductImages.FirstOrDefault(img => img.IsPrimary)?.ImageUrl,
            i.Product.ProductVariants.Any() ? i.Product.ProductVariants.Min(v => v.Price) : 0,
            i.Product.ProductVariants.Any() ? i.Product.ProductVariants.Max(v => v.Price) : 0,
            i.CreatedAt
        )).ToList();

        return new WishlistResponse(0, responseItems);
    }

    public async Task<WishlistResponse> AddToWishlistAsync(int userId, int productId)
    {
        var existing = await _wishlistRepository.GetByUserAndProductAsync(userId, productId);
        if (existing is not null)
            throw new WishlistException(400, "Sản phẩm đã có trong danh sách yêu thích.");

        var item = new WishlistItem
        {
            UserId = userId,
            ProductId = productId,
            CreatedAt = DateTime.UtcNow
        };

        await _wishlistRepository.AddAsync(item);
        await _wishlistRepository.SaveChangesAsync();
        return await GetWishlistAsync(userId);
    }

    public async Task<WishlistResponse> RemoveFromWishlistAsync(int userId, int productId)
    {
        var item = await _wishlistRepository.GetByUserAndProductAsync(userId, productId)
            ?? throw new WishlistException(404, "Sản phẩm không có trong danh sách yêu thích.");

        await _wishlistRepository.RemoveAsync(item);
        await _wishlistRepository.SaveChangesAsync();
        return await GetWishlistAsync(userId);
    }

    public async Task<CartResponse> MoveToCartAsync(int userId, int productId)
    {
        var item = await _wishlistRepository.GetByUserAndProductAsync(userId, productId)
            ?? throw new WishlistException(404, "Sản phẩm không có trong danh sách yêu thích.");

        // FIX: trước đây luôn lấy FirstOrDefault() — có thể rơi vào biến thể đã hết
        // hàng, khiến người dùng bị báo lỗi tồn kho khó hiểu khi "chuyển vào giỏ hàng"
        // từ trang Wishlist (nơi không hiển thị tồn kho từng biến thể). Giờ ưu tiên
        // biến thể còn hàng; chỉ rơi về biến thể đầu tiên nếu không còn biến thể nào
        // còn hàng (để vẫn báo lỗi tồn kho rõ ràng ở CartService thay vì im lặng bỏ qua).
        var variant = item.Product.ProductVariants.FirstOrDefault(v => v.StockQuantity > 0)
            ?? item.Product.ProductVariants.FirstOrDefault();
        if (variant is null)
            throw new WishlistException(400, "Sản phẩm không có biến thể nào.");

        await _cartService.AddToCartAsync(userId, new AddToCartRequest(variant.Id, 1));
        await _wishlistRepository.RemoveAsync(item);
        await _wishlistRepository.SaveChangesAsync();
        return await _cartService.GetCartAsync(userId);
    }
}