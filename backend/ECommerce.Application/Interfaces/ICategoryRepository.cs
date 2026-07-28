using ECommerce.Domain;

namespace ECommerce.Application;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(int id);
    Task<List<Category>> GetAllAsync(bool includeInactive = false);
    Task<bool> ExistsByNameAsync(string name, int? parentId, int? excludeId = null);
    Task<bool> HasActiveProductsAsync(int categoryId);
    Task AddAsync(Category category);
    Task UpdateAsync(Category category);
    Task SaveChangesAsync();
}
