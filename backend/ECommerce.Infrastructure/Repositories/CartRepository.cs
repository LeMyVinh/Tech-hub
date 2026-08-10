using ECommerce.Application;
using ECommerce.Domain;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Repositories;

public class CartRepository : ICartRepository
{
    private readonly Data.AppDbContext _context;

    public CartRepository(Data.AppDbContext context)
    {
        _context = context;
    }

    public async Task<Cart?> GetByUserIdAsync(int userId)
    {
        return await _context.Carts
            .Include(c => c.CartItems)
                .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.ProductImages)
            .FirstOrDefaultAsync(c => c.UserId == userId);
    }

    public async Task AddAsync(Cart cart)
    {
        await _context.Carts.AddAsync(cart);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<Cart> EnsureCartAsync(int userId)
    {
        var existing = await GetByUserIdAsync(userId);
        if (existing is not null) return existing;

        try
        {
            var cart = new Cart { UserId = userId, CreatedAt = DateTime.UtcNow };
            await _context.Carts.AddAsync(cart);
            await _context.SaveChangesAsync();
            return cart;
        }
        catch (DbUpdateException)
        {
            // RACE FIX: double-click "Thêm vào giỏ hàng" lần đầu tiên (user chưa có
            // Cart) có thể gửi 2 request gần như đồng thời, cả hai cùng thấy chưa có
            // Cart (GetByUserIdAsync ở trên trả null cho cả hai) rồi cùng insert -> vi
            // phạm unique index Cart.UserId -> DbUpdateException. Trước đây lỗi này lọt
            // thẳng lên client thành response 500 thô. Coi như request kia đã tạo Cart
            // trước, chỉ cần load lại.
            _context.ChangeTracker.Clear();
            return await GetByUserIdAsync(userId)
                ?? throw new InvalidOperationException("Không thể khởi tạo giỏ hàng.");
        }
    }

    public async Task AddOrIncrementItemAsync(int cartId, int variantId, int quantity)
    {
        // B1: thử cộng dồn nếu item đã tồn tại (UPDATE nguyên tử).
        var affected = await _context.CartItems
            .Where(ci => ci.CartId == cartId && ci.ProductVariantId == variantId)
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(ci => ci.Quantity, ci => ci.Quantity + quantity));

        if (affected > 0) return;

        // B2: item chưa tồn tại -> thử insert mới.
        try
        {
            await _context.CartItems.AddAsync(new CartItem
            {
                CartId = cartId,
                ProductVariantId = variantId,
                Quantity = quantity
            });
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            _context.ChangeTracker.Clear();

            var retried = await _context.CartItems
                .Where(ci => ci.CartId == cartId && ci.ProductVariantId == variantId)
                .ExecuteUpdateAsync(setters =>
                    setters.SetProperty(ci => ci.Quantity, ci => ci.Quantity + quantity));

            if (retried == 0)
                throw; // lỗi khác, không phải do trùng key -> giữ nguyên lỗi gốc
        }
    }
}