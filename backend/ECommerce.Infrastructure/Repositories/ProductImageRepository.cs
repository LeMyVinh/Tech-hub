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

    public Task SoftDeleteAsync(ProductImage image)
    {
        image.IsDeleted = true;
        image.DeletedAt = DateTime.UtcNow;
        _db.ProductImages.Update(image);
        return Task.CompletedTask;
    }

    public Task SoftDeleteRangeAsync(IEnumerable<ProductImage> images)
    {
        var now = DateTime.UtcNow;
        foreach (var img in images)
        {
            img.IsDeleted = true;
            img.DeletedAt = now;
        }
        _db.ProductImages.UpdateRange(images);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }
}
