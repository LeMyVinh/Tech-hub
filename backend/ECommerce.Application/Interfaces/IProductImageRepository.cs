using ECommerce.Domain;

namespace ECommerce.Application;

public interface IProductImageRepository
{
    Task<ProductImage?> GetByIdAsync(int id);
    Task<List<ProductImage>> GetByProductIdAsync(int productId);
    Task AddAsync(ProductImage image);
    Task AddRangeAsync(IEnumerable<ProductImage> images);
    Task UpdateAsync(ProductImage image);
    Task DeleteAsync(ProductImage image);
    Task DeleteRangeAsync(IEnumerable<ProductImage> images);
    Task SaveChangesAsync();
}
