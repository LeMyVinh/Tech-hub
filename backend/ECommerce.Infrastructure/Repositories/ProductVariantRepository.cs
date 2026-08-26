using ECommerce.Application;
using ECommerce.Domain;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Repositories;

public sealed class ProductVariantRepository : IProductVariantRepository
{
    private readonly AppDbContext _db;

    public ProductVariantRepository(AppDbContext db)
    {
        _db = db;
    }

    // FIX (#1 - checkout sản phẩm đã bị ẩn): trước đây không Include(Product), nên
    // CartService/OrderService không có cách nào biết variant đang thuộc sản phẩm đã
    // bị Admin chuyển Status="Inactive". Một sản phẩm được thêm vào giỏ TRƯỚC KHI bị
    // ẩn vẫn checkout được bình thường vì code chỉ kiểm tra tồn kho, không kiểm tra
    // trạng thái kinh doanh. Include ở đây để mọi nơi gọi GetByIdAsync đều có sẵn
    // variant.Product.Status để kiểm tra.
    public async Task<ProductVariant?> GetByIdAsync(int id)
    {
        return await _db.ProductVariants
            .Include(v => v.Product)
            .FirstOrDefaultAsync(v => v.Id == id);
    }

    public async Task<ProductVariant?> GetByIdWithProductAsync(int id)
    {
        return await GetByIdAsync(id);
    }

    public async Task<List<ProductVariant>> GetByProductIdAsync(int productId)
    {
        return await _db.ProductVariants.Where(v => v.ProductId == productId).ToListAsync();
    }

    public async Task AddAsync(ProductVariant variant)
    {
        await _db.ProductVariants.AddAsync(variant);
    }

    public async Task AddRangeAsync(IEnumerable<ProductVariant> variants)
    {
        await _db.ProductVariants.AddRangeAsync(variants);
    }

    public Task UpdateAsync(ProductVariant variant)
    {
        _db.ProductVariants.Update(variant);
        return Task.CompletedTask;
    }

    public Task SoftDeleteAsync(ProductVariant variant)
    {
        variant.IsDeleted = true;
        variant.DeletedAt = DateTime.UtcNow;
        _db.ProductVariants.Update(variant);
        return Task.CompletedTask;
    }

    public Task SoftDeleteRangeAsync(IEnumerable<ProductVariant> variants)
    {
        var now = DateTime.UtcNow;
        foreach (var v in variants)
        {
            v.IsDeleted = true;
            v.DeletedAt = now;
        }
        _db.ProductVariants.UpdateRange(variants);
        return Task.CompletedTask;
    }

    // RACE-CONDITION FIX (BR-02): UPDATE nguyên tử, điều kiện StockQuantity >= quantity
    // được kiểm tra ngay trong câu SQL (không phải trong C#), nên DB quyết định ai
    // "thắng" khi có nhiều request cùng trừ kho một variant, thay vì 2 request cùng đọc
    // được số dư cũ rồi cùng ghi đè (oversell).
    public async Task<bool> TryDecrementStockAsync(int variantId, int quantity)
    {
        var affected = await _db.ProductVariants
            .Where(v => v.Id == variantId && v.StockQuantity >= quantity)
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(v => v.StockQuantity, v => v.StockQuantity - quantity));

        return affected > 0;
    }

    public async Task<bool> HasOrdersAsync(int productVariantId)
    {
        return await _db.OrderItems
            .AnyAsync(x => x.ProductVariantId == productVariantId);
    }

    // FIX: kiểm tra variant có đang tồn tại trong CartItem của bất kỳ khách hàng nào
    // (dù chưa từng phát sinh đơn hàng). CartItem.ProductVariantId là FK not-null, nên
    // xóa cứng một variant đang bị tham chiếu sẽ ném DbUpdateException không kiểm soát.
    public async Task<bool> HasCartItemsAsync(int productVariantId)
    {
        return await _db.CartItems
            .AnyAsync(ci => ci.ProductVariantId == productVariantId);
    }

    public async Task IncrementStockAsync(int variantId, int quantity)
    {
        await _db.ProductVariants
            .Where(v => v.Id == variantId)
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(v => v.StockQuantity, v => v.StockQuantity + quantity));
    }

    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }
}