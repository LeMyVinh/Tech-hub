using ECommerce.Application;
using ECommerce.Domain;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Repositories;

public sealed class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _db;

    public CategoryRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Category?> GetByIdAsync(int id)
    {
        return await _db.Categories
            .Include(c => c.Parent)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<List<Category>> GetAllAsync(bool includeInactive = false)
    {
        var query = _db.Categories
            .Include(c => c.Parent)
            .AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(c => c.IsActive == true);
        }

        return await query.ToListAsync();
    }

    public async Task<bool> ExistsByNameAsync(string name, int? parentId, int? excludeId = null)
    {
        var lowerName = name.Trim().ToLower();
        return await _db.Categories.AnyAsync(c =>
            c.Name.ToLower() == lowerName &&
            c.ParentId == parentId &&
            (excludeId == null || c.Id != excludeId));
    }

    public async Task<bool> HasActiveProductsAsync(int categoryId)
    {
        return await _db.Products.AnyAsync(p => p.CategoryId == categoryId && p.Status == "Active");
    }

    public async Task AddAsync(Category category)
    {
        await _db.Categories.AddAsync(category);
    }

    public Task UpdateAsync(Category category)
    {
        _db.Categories.Update(category);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }
}
