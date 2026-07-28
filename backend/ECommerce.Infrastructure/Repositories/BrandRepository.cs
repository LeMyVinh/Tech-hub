using ECommerce.Application;
using ECommerce.Domain;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Repositories;

public sealed class BrandRepository : IBrandRepository
{
    private readonly AppDbContext _db;

    public BrandRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Brand?> GetByIdAsync(int id)
    {
        return await _db.Brands.FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<List<Brand>> GetAllAsync(bool includeInactive = false)
    {
        var query = _db.Brands.AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(b => b.IsActive == true);
        }

        return await query.ToListAsync();
    }

    public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
    {
        var lowerName = name.Trim().ToLower();
        return await _db.Brands.AnyAsync(b =>
            b.Name.ToLower() == lowerName &&
            (excludeId == null || b.Id != excludeId));
    }

    public async Task<bool> HasActiveProductsAsync(int brandId)
    {
        return await _db.Products.AnyAsync(p => p.BrandId == brandId && p.Status == "Active");
    }

    public async Task AddAsync(Brand brand)
    {
        await _db.Brands.AddAsync(brand);
    }

    public Task UpdateAsync(Brand brand)
    {
        _db.Brands.Update(brand);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }
}
