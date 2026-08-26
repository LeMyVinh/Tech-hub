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

    // SOFT DELETE: set IsDeleted=true + DeletedAt=UtcNow. Service nên check
    // HasOrdersAsync + HasCartItemsAsync trước — variant còn nằm trong giỏ hàng
    // hoặc đơn chưa giao thì chặn.
    Task SoftDeleteAsync(ProductVariant variant);
    Task SoftDeleteRangeAsync(IEnumerable<ProductVariant> variants);
    Task<bool> TryDecrementStockAsync(int variantId, int quantity);
    Task<bool> HasOrdersAsync(int productVariantId);

    // FIX: kiểm tra variant có đang nằm trong giỏ hàng của bất kỳ khách nào không.
    // HasOrdersAsync chỉ check bảng OrderItem, KHÔNG check CartItem — nếu xóa cứng
    // một variant đang bị CartItem.ProductVariantId (FK not-null) trỏ tới, sẽ vi phạm
    // ràng buộc khóa ngoại và crash 500 khi Admin sửa/xóa biến thể sản phẩm.
    Task<bool> HasCartItemsAsync(int productVariantId);

    /// <summary>Hoàn kho nguyên tử (dùng khi hủy đơn).</summary>
    Task IncrementStockAsync(int variantId, int quantity);

    Task SaveChangesAsync();
}