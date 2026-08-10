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
    Task<bool> HasOrdersAsync(int productVariantId);

    /// <summary>
    /// STOCK RACE-CONDITION FIX: trừ kho bằng 1 câu UPDATE nguyên tử có điều kiện
    /// (StockQuantity >= quantity), bỏ qua change tracker. Trả về false nếu không đủ
    /// hàng tại thời điểm thực thi (đã bị request khác trừ trước), thay vì đọc-rồi-ghi
    /// (không atomic, có thể bị 2 request đặt hàng cùng lúc "đè" lên nhau).
    /// </summary>
    Task<bool> TryDecrementStockAsync(int variantId, int quantity);

    /// <summary>Hoàn kho nguyên tử (dùng khi hủy đơn / admin chuyển trạng thái Cancelled).</summary>
    Task IncrementStockAsync(int variantId, int quantity);

    Task SaveChangesAsync();
}