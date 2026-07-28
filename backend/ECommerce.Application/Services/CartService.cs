using ECommerce.Domain;

namespace ECommerce.Application;

public class CartService : ICartService
{
    private readonly ICartRepository _cartRepository;
    private readonly IProductVariantRepository _variantRepository;

    public CartService(ICartRepository cartRepository, IProductVariantRepository variantRepository)
    {
        _cartRepository = cartRepository;
        _variantRepository = variantRepository;
    }

    public async Task<CartResponse> GetCartAsync(int userId)
    {
        var cart = await _cartRepository.GetByUserIdAsync(userId);
        if (cart is null)
        {
            return new CartResponse(0, new List<CartItemResponse>(), 0);
        }
        return MapToResponse(cart);
    }

    public async Task<CartResponse> AddToCartAsync(int userId, AddToCartRequest request)
    {
        var variant = await _variantRepository.GetByIdAsync(request.VariantId)
            ?? throw new CartException(404, "Sản phẩm không tồn tại.");

        if (variant.StockQuantity < request.Quantity)
            throw new CartException(400, $"Số lượng tồn kho không đủ. Chỉ còn {variant.StockQuantity} sản phẩm.");

        var cart = await _cartRepository.GetByUserIdAsync(userId);
        if (cart is null)
        {
            cart = new Cart
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                CartItems = new List<CartItem>()
            };
            await _cartRepository.AddAsync(cart);
        }

        var existingItem = cart.CartItems.FirstOrDefault(i => i.ProductVariantId == request.VariantId);
        if (existingItem is not null)
        {
            var newQuantity = existingItem.Quantity + request.Quantity;
            if (newQuantity > variant.StockQuantity)
                throw new CartException(400, $"Số lượng tồn kho không đủ. Chỉ còn {variant.StockQuantity} sản phẩm.");
            existingItem.Quantity = newQuantity;
        }
        else
        {
            cart.CartItems.Add(new CartItem
            {
                CartId = cart.Id,
                ProductVariantId = request.VariantId,
                Quantity = request.Quantity
            });
        }

        await _cartRepository.SaveChangesAsync();
        return MapToResponse(cart);
    }

    public async Task<CartResponse> UpdateCartItemAsync(int userId, int itemId, UpdateCartItemRequest request)
    {
        var cart = await _cartRepository.GetByUserIdAsync(userId)
            ?? throw new CartException(404, "Giỏ hàng trống.");

        var item = cart.CartItems.FirstOrDefault(i => i.Id == itemId)
            ?? throw new CartException(404, "Sản phẩm không có trong giỏ hàng.");

        var variant = await _variantRepository.GetByIdAsync(item.ProductVariantId)
            ?? throw new CartException(404, "Sản phẩm không tồn tại.");

        if (request.Quantity <= 0)
        {
            cart.CartItems.Remove(item);
        }
        else
        {
            if (request.Quantity > variant.StockQuantity)
                throw new CartException(400, $"Số lượng tồn kho không đủ. Chỉ còn {variant.StockQuantity} sản phẩm.");
            item.Quantity = request.Quantity;
        }

        await _cartRepository.SaveChangesAsync();
        return MapToResponse(cart);
    }

    public async Task<CartResponse> RemoveFromCartAsync(int userId, int itemId)
    {
        var cart = await _cartRepository.GetByUserIdAsync(userId)
            ?? throw new CartException(404, "Giỏ hàng trống.");

        var item = cart.CartItems.FirstOrDefault(i => i.Id == itemId)
            ?? throw new CartException(404, "Sản phẩm không có trong giỏ hàng.");

        cart.CartItems.Remove(item);
        await _cartRepository.SaveChangesAsync();
        return MapToResponse(cart);
    }

    public async Task<CartResponse> ClearCartAsync(int userId)
    {
        var cart = await _cartRepository.GetByUserIdAsync(userId);
        if (cart is not null)
        {
            cart.CartItems.Clear();
            await _cartRepository.SaveChangesAsync();
        }
        return new CartResponse(cart?.Id ?? 0, new List<CartItemResponse>(), 0);
    }

    private static CartResponse MapToResponse(Cart cart)
    {
        var items = cart.CartItems.Select(i => new CartItemResponse(
            i.Id,
            i.ProductVariantId,
            i.ProductVariant.Product.Name,
            i.ProductVariant.VariantName,
            i.ProductVariant.Sku,
            i.ProductVariant.Price,
            i.Quantity,
            i.ProductVariant.StockQuantity,
            i.ProductVariant.Product.ProductImages.FirstOrDefault(img => img.IsPrimary)?.ImageUrl,
            i.ProductVariant.Price * i.Quantity
        )).ToList();

        var totalAmount = items.Sum(i => i.Subtotal);
        return new CartResponse(cart.Id, items, totalAmount);
    }
}
