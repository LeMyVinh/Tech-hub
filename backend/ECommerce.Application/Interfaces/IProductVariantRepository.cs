using ECommerce.Domain;

namespace ECommerce.Application;

public interface IProductVariantRepository
{
    Task<ProductVariant?> GetByIdAsync(int id);

    /// <summary>Lấy kèm Product để kiểm tra Product.Status trước khi cho checkout.</summary>
    Task<ProductVariant?> GetByIdWithProductAsync(int id);

    Task<List<ProductVariant>> GetByProductIdAsync(int productId);
    Task AddAsync(ProductVariant variant);
    Task AddRangeAsync(IEnumerable<ProductVariant> variants);
    Task UpdateAsync(ProductVariant variant);
    Task DeleteAsync(ProductVariant variant);
    Task DeleteRangeAsync(IEnumerable<ProductVariant> variants);
    Task<bool> TryDecrementStockAsync(int variantId, int quantity);
    Task<bool> HasOrdersAsync(int productVariantId);

    /// <summary>Hoàn kho nguyên tử (dùng khi hủy đơn).</summary>
    Task IncrementStockAsync(int variantId, int quantity);

    Task SaveChangesAsync();
}