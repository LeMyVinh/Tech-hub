using ECommerce.Domain;

namespace ECommerce.Application;

public interface IProductImageRepository
{
    Task<ProductImage?> GetByIdAsync(int id);
    Task<List<ProductImage>> GetByProductIdAsync(int productId);
    Task AddAsync(ProductImage image);
    Task AddRangeAsync(IEnumerable<ProductImage> images);
    Task UpdateAsync(ProductImage image);

    // SOFT DELETE: set IsDeleted=true + DeletedAt=UtcNow. ProductImage chỉ là media
    // nên an toàn để xóa mềm mà không cần check FK.
    Task SoftDeleteAsync(ProductImage image);
    Task SoftDeleteRangeAsync(IEnumerable<ProductImage> images);

    Task SaveChangesAsync();
}
