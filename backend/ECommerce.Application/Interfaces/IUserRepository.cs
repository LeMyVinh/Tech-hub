using ECommerce.Domain;

namespace ECommerce.Application;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(int id);
    // Lấy cả user đã soft delete (bỏ HasQueryFilter) — dùng cho trang admin khi
    // cần xem user đã xóa + phục vụ Restore.
    Task<User?> GetByIdIncludingDeletedAsync(int id);
    Task<List<User>> GetAllAsync(int page, int pageSize);
    Task<int> GetCountAsync();
    Task<int> GetActiveAdminCountAsync();

    Task AddAsync(User user);

    // SOFT DELETE: set IsDeleted=true + DeletedAt=UtcNow. Service nên kiểm tra ràng
    // buộc nghiệp vụ (vd: còn đơn hàng chưa hoàn tất không) trước khi gọi.
    Task SoftDeleteAsync(User user);

    // RESTORE: đảo ngược soft delete. User hoạt động lại bình thường.
    Task RestoreAsync(User user);

    Task SaveChangesAsync();
}