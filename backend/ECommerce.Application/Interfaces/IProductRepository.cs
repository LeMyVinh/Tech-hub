using ECommerce.Domain;

namespace ECommerce.Application;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id, bool includeInactive = false, bool includeDeleted = false);
    Task<Product?> GetWithDetailsAsync(int id, bool includeInactive = false, bool includeDeleted = false);
    Task<PagedResult<ProductSummaryResponse>> SearchAsync(ProductFilterParams filter, bool includeInactive = false, bool includeDeleted = false);
    Task<bool> ExistsBySkuAsync(string sku, int? excludeVariantId = null);
    Task<bool> HasOrdersAsync(int productId);
    Task AddAsync(Product product);
    Task UpdateAsync(Product product);

    // SOFT DELETE: set IsDeleted=true + DeletedAt=UtcNow. Service nên check
    // HasOrdersAsync trước — nếu còn OrderItem lịch sử thì vẫn cho xóa mềm
    // (giữ tham chiếu cho đơn cũ), nhưng cảnh báo admin.
    Task SoftDeleteAsync(Product product);
    Task RestoreAsync(Product product);

    Task SaveChangesAsync();
}
