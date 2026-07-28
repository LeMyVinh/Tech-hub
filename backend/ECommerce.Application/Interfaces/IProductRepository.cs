using ECommerce.Domain;

namespace ECommerce.Application;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id, bool includeInactive = false);
    Task<Product?> GetWithDetailsAsync(int id, bool includeInactive = false);
    Task<PagedResult<ProductSummaryResponse>> SearchAsync(ProductFilterParams filter, bool includeInactive = false);
    Task<bool> ExistsBySkuAsync(string sku, int? excludeVariantId = null);
    Task<bool> HasOrdersAsync(int productId);
    Task AddAsync(Product product);
    Task UpdateAsync(Product product);
    Task DeleteAsync(Product product);
    Task SaveChangesAsync();
}
