using ECommerce.Domain;

namespace ECommerce.Application;

public interface IReviewRepository
{
    Task<Review?> GetByIdAsync(int id);

    // SOFT DELETE: lấy cả review đã xóa mềm (dùng cho thao tác Xóa/Khôi phục ở
    // trang quản trị, bỏ qua HasQueryFilter mặc định).
    Task<Review?> GetByIdIncludingDeletedAsync(int id);

    Task<Review?> GetByOrderItemIdAsync(int orderItemId);
    Task<List<Review>> GetByProductIdAsync(int productId, int page, int pageSize);
    Task<int> GetByProductIdCountAsync(int productId);
    Task<List<Review>> GetPendingReviewsAsync(int page, int pageSize);
    Task<int> GetPendingReviewsCountAsync();

    // ADMIN: lấy TOÀN BỘ đánh giá (mọi trạng thái, bao gồm cả đã xóa mềm) để hiển
    // thị ở trang quản trị — bản ghi đã xóa vẫn hiện nhưng bị làm mờ ở FE.
    Task<List<Review>> GetAllReviewsAsync(int page, int pageSize);
    Task<int> GetAllReviewsCountAsync();

    Task<double> GetAverageRatingAsync(int productId);
    Task AddAsync(Review review);

    // SOFT DELETE: set IsDeleted=true + DeletedAt=UtcNow.
    Task SoftDeleteAsync(Review review);

    // RESTORE: đảo ngược soft delete.
    Task RestoreAsync(Review review);

    Task SaveChangesAsync();
}