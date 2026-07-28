using ECommerce.Domain;

namespace ECommerce.Application;

public interface IBrandRepository
{
    Task<Brand?> GetByIdAsync(int id);
    Task<List<Brand>> GetAllAsync(bool includeInactive = false);
    Task<bool> ExistsByNameAsync(string name, int? excludeId = null);
    Task<bool> HasActiveProductsAsync(int brandId);
    Task AddAsync(Brand brand);
    Task UpdateAsync(Brand brand);
    Task SaveChangesAsync();
}
