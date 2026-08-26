using ECommerce.Domain;

namespace ECommerce.Application;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(int id, bool includeDeleted = false);
    Task<List<Category>> GetAllAsync(bool includeInactive = false, bool includeDeleted = false);
    Task<bool> ExistsByNameAsync(string name, int? parentId, int? excludeId = null);
    Task<bool> HasActiveProductsAsync(int categoryId);
    Task AddAsync(Category category);
    Task UpdateAsync(Category category);

    // SOFT DELETE: set IsDeleted=true + DeletedAt=UtcNow. Service nên check
    // HasActiveProductsAsync trước — nếu còn Product Active chưa xóa thì báo lỗi.
    Task SoftDeleteAsync(Category category);
    Task RestoreAsync(Category category);

    Task SaveChangesAsync();
}
