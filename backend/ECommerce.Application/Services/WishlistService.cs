using ECommerce.Domain;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application;

public class WishlistService : IWishlistService
{
    private readonly IWishlistRepository _wishlistRepository;
    private readonly ICartService _cartService;
    private readonly IProductRepository _productRepository;

    public WishlistService(
        IWishlistRepository wishlistRepository,
        ICartService cartService,
        IProductRepository productRepository)
    {
        _wishlistRepository = wishlistRepository;
        _cartService = cartService;
        _productRepository = productRepository;
    }

    public async Task<WishlistResponse> GetWishlistAsync(int userId, bool includeDeleted = false)
    {
        var items = await _wishlistRepository.GetByUserIdAsync(userId, includeDeleted);
        var responseItems = items.Select(i => new WishlistItemResponse(
            i.Id,
            i.ProductId,
            i.Product.Name,
            i.Product.ProductImages.FirstOrDefault(img => img.IsPrimary)?.ImageUrl,
            i.Product.ProductVariants.Any() ? i.Product.ProductVariants.Min(v => v.Price) : 0,
            i.Product.ProductVariants.Any() ? i.Product.ProductVariants.Max(v => v.Price) : 0,
            i.CreatedAt,
            i.IsDeleted,
            i.DeletedAt
        )).ToList();

        return new WishlistResponse(0, responseItems);
    }

    public async Task<WishlistResponse> AddToWishlistAsync(int userId, int productId)
    {
        var product = await _productRepository.GetByIdAsync(productId, includeInactive: true)
            ?? throw new WishlistException(404, "Sản phẩm không tồn tại.");

        if (product.Status != "Active")
            throw new WishlistException(400, "Sản phẩm hiện không còn kinh doanh.");

        var existing = await _wishlistRepository.GetByUserAndProductAsync(userId, productId, includeDeleted: true);
        if (existing is not null && !existing.IsDeleted)
            throw new WishlistException(400, "Sản phẩm đã có trong danh sách yêu thích.");
        if (existing is not null)
        {
            await _wishlistRepository.RestoreAsync(existing);
            await _wishlistRepository.SaveChangesAsync();
            return await GetWishlistAsync(userId);
        }

        var item = new WishlistItem
        {
            UserId = userId,
            ProductId = productId,
            CreatedAt = DateTime.UtcNow
        };

        await _wishlistRepository.AddAsync(item);

        try
        {
            await _wishlistRepository.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // RACE FIX (#2 - double-click nút "Yêu thích"): 2 request cùng lúc đều đọc
            // "chưa có trong wishlist" (bước GetByUserAndProductAsync ở trên) và cùng cố
            // INSERT, va vào uq_wishlist_user_product (UserId, ProductId UNIQUE). Trước
            // đây exception này không được bắt -> lỗi 500 thô. Vì request kia đã thêm
            // thành công, request này chỉ cần bỏ qua (không coi là lỗi) và trả về
            // wishlist mới nhất.
        }

        return await GetWishlistAsync(userId);
    }

    public async Task<WishlistResponse> RemoveFromWishlistAsync(int userId, int productId)
    {
        var item = await _wishlistRepository.GetByUserAndProductAsync(userId, productId)
            ?? throw new WishlistException(404, "Sản phẩm không có trong danh sách yêu thích.");

        await _wishlistRepository.SoftDeleteAsync(item);
        await _wishlistRepository.SaveChangesAsync();
        return await GetWishlistAsync(userId);
    }

    public async Task<WishlistResponse> RestoreWishlistItemAsync(int userId, int productId)
    {
        var item = await _wishlistRepository.GetByUserAndProductAsync(userId, productId, includeDeleted: true)
            ?? throw new WishlistException(404, "Sản phẩm không có trong danh sách yêu thích.");
        if (!item.IsDeleted)
            throw new WishlistException(400, "Sản phẩm này chưa bị xóa.");

        await _wishlistRepository.RestoreAsync(item);
        await _wishlistRepository.SaveChangesAsync();
        return await GetWishlistAsync(userId, includeDeleted: true);
    }

    public async Task<CartResponse> MoveToCartAsync(int userId, int productId)
    {
        var item = await _wishlistRepository.GetByUserAndProductAsync(userId, productId)
            ?? throw new WishlistException(404, "Sản phẩm không có trong danh sách yêu thích.");

        var variant = item.Product.ProductVariants.FirstOrDefault(v => v.StockQuantity > 0)
            ?? item.Product.ProductVariants.FirstOrDefault();
        if (variant is null)
            throw new WishlistException(400, "Sản phẩm không có biến thể nào.");

        await _cartService.AddToCartAsync(userId, new AddToCartRequest(variant.Id, 1));
        await _wishlistRepository.SoftDeleteAsync(item);
        await _wishlistRepository.SaveChangesAsync();
        return await _cartService.GetCartAsync(userId);
    }
}
