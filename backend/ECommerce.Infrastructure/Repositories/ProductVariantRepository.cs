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

    public async Task<ProductVariant?> GetByIdAsync(int id)
    {
        return await _db.ProductVariants.FirstOrDefaultAsync(v => v.Id == id);
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

    public Task DeleteAsync(ProductVariant variant)
    {
        _db.ProductVariants.Remove(variant);
        return Task.CompletedTask;
    }

    public Task DeleteRangeAsync(IEnumerable<ProductVariant> variants)
    {
        _db.ProductVariants.RemoveRange(variants);
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