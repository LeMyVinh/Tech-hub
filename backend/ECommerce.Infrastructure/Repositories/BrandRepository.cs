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

    public Task<Brand?> GetByIdIncludingDeletedAsync(int id) =>
        _db.Brands.IgnoreQueryFilters().FirstOrDefaultAsync(b => b.Id == id);

    public async Task<List<Brand>> GetAllAsync(bool includeDeleted = false)
    {
        var query = includeDeleted
            ? _db.Brands.IgnoreQueryFilters().AsQueryable()
            : _db.Brands.AsQueryable();

        return await query
            .OrderBy(b => b.IsDeleted)
            .ThenBy(b => b.Name)
            .ToListAsync();
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

    public async Task SoftDeleteAsync(Brand brand)
    {
        var now = DateTime.UtcNow;
        var rows = await _db.Brands.IgnoreQueryFilters()
            .Where(b => b.Id == brand.Id && !b.IsDeleted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(b => b.IsDeleted, true)
                .SetProperty(b => b.DeletedAt, now));

        if (rows == 0 && !brand.IsDeleted)
            throw new InvalidOperationException($"Không thể soft delete brand #{brand.Id}.");
    }

    public async Task RestoreAsync(Brand brand)
    {
        var rows = await _db.Brands.IgnoreQueryFilters()
            .Where(b => b.Id == brand.Id && b.IsDeleted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(b => b.IsDeleted, false)
                .SetProperty(b => b.DeletedAt, (DateTime?)null));

        if (rows == 0 && brand.IsDeleted)
            throw new InvalidOperationException($"Không thể khôi phục brand #{brand.Id}.");
    }

    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }
}
