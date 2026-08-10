using ECommerce.Domain;
using Microsoft.EntityFrameworkCore;

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
        if (request.Quantity <= 0)
            throw new CartException(400, "Số lượng sản phẩm phải lớn hơn 0.");

        var variant = await _variantRepository.GetByIdAsync(request.VariantId)
            ?? throw new CartException(404, "Sản phẩm không tồn tại.");

        if (variant.Product.Status != "Active")
            throw new CartException(400, "Sản phẩm hiện không còn kinh doanh.");

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

            try
            {
                await _cartRepository.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // RACE FIX (#2 - double-click "Thêm vào giỏ hàng" khi chưa có giỏ hàng):
                // Cart.UserId là UNIQUE, nên nếu 2 request cùng lúc cho user chưa có giỏ
                // hàng, cả 2 đều đọc "chưa có cart" và cùng cố tạo mới -> request thua
                // cuộc trước đây bị crash 500 thô. Giờ tải lại giỏ hàng (đã được request
                // kia tạo) thay vì lỗi.
                cart = await _cartRepository.GetByUserIdAsync(userId)
                    ?? throw new CartException(500, "Không thể tạo giỏ hàng, vui lòng thử lại.");
            }
        }

        var existingItem = cart.CartItems.FirstOrDefault(i => i.ProductVariantId == request.VariantId);
        if (existingItem is not null)
        {
            var newQuantity = existingItem.Quantity + request.Quantity;
            if (newQuantity > variant.StockQuantity)
                throw new CartException(400, $"Số lượng tồn kho không đủ. Chỉ còn {variant.StockQuantity} sản phẩm.");
            existingItem.Quantity = newQuantity;
            await _cartRepository.SaveChangesAsync();
        }
        else
        {
            // FIX (build error): dùng biến cục bộ giữ tham chiếu tới item vừa tạo, thay
            // vì cart.CartItems[^1] -- Cart.CartItems khai báo kiểu ICollection<CartItem>,
            // không phải List<CartItem>, nên KHÔNG hỗ trợ toán tử index [] (lỗi biên dịch
            // CS0021 "Cannot apply indexing with []").
            var newItem = new CartItem
            {
                CartId = cart.Id,
                ProductVariantId = request.VariantId,
                Quantity = request.Quantity
            };
            cart.CartItems.Add(newItem);

            try
            {
                await _cartRepository.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // RACE FIX (#2 - double-click "Thêm vào giỏ hàng"): 2 request cùng lúc
                // cho cùng 1 variant đều đọc "chưa có item trong giỏ" nên cả 2 cùng cố
                // INSERT, va vào uq_cartitem_cart_variant (CartId, ProductVariantId
                // UNIQUE). Trước đây exception này không được bắt -> lộ ra thành lỗi 500
                // thô ngoài ý muốn. Giờ: bỏ item vừa insert-thất-bại khỏi change tracker,
                // tải lại giỏ hàng (item của request kia đã có ở đó) và CỘNG DỒN số
                // lượng vào thay vì báo lỗi.
                cart.CartItems.Remove(newItem);

                var refreshedCart = await _cartRepository.GetByUserIdAsync(userId)
                    ?? throw new CartException(500, "Không thể thêm sản phẩm vào giỏ hàng, vui lòng thử lại.");
                var winningItem = refreshedCart.CartItems.FirstOrDefault(i => i.ProductVariantId == request.VariantId)
                    ?? throw new CartException(500, "Không thể thêm sản phẩm vào giỏ hàng, vui lòng thử lại.");

                var mergedQuantity = winningItem.Quantity + request.Quantity;
                if (mergedQuantity > variant.StockQuantity)
                    throw new CartException(400, $"Số lượng tồn kho không đủ. Chỉ còn {variant.StockQuantity} sản phẩm.");

                winningItem.Quantity = mergedQuantity;
                await _cartRepository.SaveChangesAsync();
            }
        }

        var refreshed = await _cartRepository.GetByUserIdAsync(userId);
        return MapToResponse(refreshed!);
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
            if (variant.Product.Status != "Active" && request.Quantity > item.Quantity)
                throw new CartException(400, "Sản phẩm hiện không còn kinh doanh, không thể tăng số lượng.");

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