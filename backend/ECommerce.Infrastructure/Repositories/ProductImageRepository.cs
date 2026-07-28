using ECommerce.Application;
using ECommerce.Domain;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Repositories;

public sealed class ProductImageRepository : IProductImageRepository
{
    private readonly AppDbContext _db;

    public ProductImageRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ProductImage?> GetByIdAsync(int id)
    {
        return await _db.ProductImages.FirstOrDefaultAsync(img => img.Id == id);
    }

    public async Task<List<ProductImage>> GetByProductIdAsync(int productId)
    {
        return await _db.ProductImages.Where(img => img.ProductId == productId).ToListAsync();
    }

    public async Task AddAsync(ProductImage image)
    {
        await _db.ProductImages.AddAsync(image);
    }

    public async Task AddRangeAsync(IEnumerable<ProductImage> images)
    {
        await _db.ProductImages.AddRangeAsync(images);
    }

    public Task UpdateAsync(ProductImage image)
    {
        _db.ProductImages.Update(image);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(ProductImage image)
    {
        _db.ProductImages.Remove(image);
        return Task.CompletedTask;
    }

    public Task DeleteRangeAsync(IEnumerable<ProductImage> images)
    {
        _db.ProductImages.RemoveRange(images);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }
}
