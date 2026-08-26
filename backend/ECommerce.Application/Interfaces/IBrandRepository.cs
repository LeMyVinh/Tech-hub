using ECommerce.Domain;

namespace ECommerce.Application;

public interface IBrandRepository
{
    Task<Brand?> GetByIdAsync(int id);
    Task<Brand?> GetByIdIncludingDeletedAsync(int id);
    Task<List<Brand>> GetAllAsync(bool includeDeleted = false);
    Task<bool> ExistsByNameAsync(string name, int? excludeId = null);
    Task<bool> HasActiveProductsAsync(int brandId);
    Task AddAsync(Brand brand);
    Task UpdateAsync(Brand brand);

    // SOFT DELETE: set IsDeleted=true + DeletedAt=UtcNow.
    Task SoftDeleteAsync(Brand brand);

    Task RestoreAsync(Brand brand);

    Task SaveChangesAsync();
}
