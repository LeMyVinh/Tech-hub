using ECommerce.Domain;

namespace ECommerce.Application;

public interface IProductVariantRepository
{
    Task<ProductVariant?> GetByIdAsync(int id);
    Task<List<ProductVariant>> GetByProductIdAsync(int productId);
    Task AddAsync(ProductVariant variant);
    Task AddRangeAsync(IEnumerable<ProductVariant> variants);
    Task UpdateAsync(ProductVariant variant);
    Task DeleteAsync(ProductVariant variant);
    Task DeleteRangeAsync(IEnumerable<ProductVariant> variants);
    Task SaveChangesAsync();
}
