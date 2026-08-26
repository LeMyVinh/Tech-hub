using ECommerce.Application;
using ECommerce.Domain;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Repositories;

public class ReviewRepository : IReviewRepository
{
    private readonly Data.AppDbContext _context;

    public ReviewRepository(Data.AppDbContext context)
    {
        _context = context;
    }

    public async Task<Review?> GetByIdAsync(int id)
    {
        return await _context.Reviews
            .Include(r => r.User)
            .Include(r => r.ReviewImages)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    // SOFT DELETE: IgnoreQueryFilters() để vẫn tìm được review đã bị xóa mềm —
    // cần thiết cho cả 2 chiều: xóa (kiểm tra chưa xóa trước đó) và khôi phục
    // (bắt buộc phải bỏ qua filter vì review đã xóa sẽ không lọt qua filter mặc định).
    public async Task<Review?> GetByIdIncludingDeletedAsync(int id)
    {
        return await _context.Reviews
            .IgnoreQueryFilters()
            .Include(r => r.User)
            .Include(r => r.Product)
            .Include(r => r.ReviewImages)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<Review?> GetByOrderItemIdAsync(int orderItemId)
    {
        return await _context.Reviews.FirstOrDefaultAsync(r => r.OrderItemId == orderItemId);
    }

    public async Task<List<Review>> GetByProductIdAsync(int productId, int page, int pageSize)
    {
        return await _context.Reviews
            .Include(r => r.User)
            .Include(r => r.ReviewImages)
            .Where(r => r.ProductId == productId && r.Status == "Approved")
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetByProductIdCountAsync(int productId)
    {
        return await _context.Reviews.CountAsync(r => r.ProductId == productId && r.Status == "Approved");
    }

    public async Task<List<Review>> GetPendingReviewsAsync(int page, int pageSize)
    {
        return await _context.Reviews
            .Include(r => r.User)
            .Include(r => r.Product)
            .Include(r => r.ReviewImages)
            .Where(r => r.Status == "Pending")
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetPendingReviewsCountAsync()
    {
        return await _context.Reviews.CountAsync(r => r.Status == "Pending");
    }

    // ADMIN: lấy toàn bộ đánh giá (mọi Status, bao gồm cả IsDeleted=true) — dùng
    // IgnoreQueryFilters() để review đã xóa mềm vẫn hiện lên trang quản trị (làm
    // mờ + có nút Khôi phục), không giống các trang public chỉ thấy review chưa xóa.
    // Sắp xếp: review chưa xóa lên trước, mới nhất lên đầu.
    public async Task<List<Review>> GetAllReviewsAsync(int page, int pageSize)
    {
        return await _context.Reviews
            .IgnoreQueryFilters()
            .Include(r => r.User)
            .Include(r => r.Product)
            .Include(r => r.ReviewImages)
            .OrderBy(r => r.IsDeleted)
            .ThenByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetAllReviewsCountAsync()
    {
        return await _context.Reviews.IgnoreQueryFilters().CountAsync();
    }

    // PERF FIX (#4): trước đây hàm này tải TOÀN BỘ các dòng Review (Approved) của sản
    // phẩm vào bộ nhớ ứng dụng (ToListAsync) chỉ để tính trung bình một cột Rating
    // bằng LINQ-to-Objects. Với sản phẩm có nhiều review, đây là lượng dữ liệu
    // không cần thiết phải kéo qua network + serialize. Giờ dùng AverageAsync để
    // SQL Server/MySQL tính AVG() trực tiếp, chỉ trả về một giá trị số duy nhất.
    // Global Query Filter (!IsDeleted) tự động áp dụng nên review đã xóa mềm
    // không được tính vào điểm trung bình.
    public async Task<double> GetAverageRatingAsync(int productId)
    {
        var query = _context.Reviews
            .Where(r => r.ProductId == productId && r.Status == "Approved");

        var hasAny = await query.AnyAsync();
        if (!hasAny) return 0;

        return await query.AverageAsync(r => (double)r.Rating);
    }

    public async Task AddAsync(Review review)
    {
        await _context.Reviews.AddAsync(review);
    }

    // SOFT DELETE: UPDATE nguyên tử bằng ExecuteUpdateAsync (giống Brand/Category/
    // User), tránh việc change tracker không ghi nhận đúng nếu entity được load từ
    // một context/scope khác.
    public async Task SoftDeleteAsync(Review review)
    {
        var now = DateTime.UtcNow;
        var rows = await _context.Reviews.IgnoreQueryFilters()
            .Where(r => r.Id == review.Id && !r.IsDeleted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(r => r.IsDeleted, true)
                .SetProperty(r => r.DeletedAt, now));

        if (rows == 0 && !review.IsDeleted)
            throw new InvalidOperationException($"Không thể xóa đánh giá #{review.Id}.");

        review.IsDeleted = true;
        review.DeletedAt = now;
    }

    // RESTORE: đảo ngược soft delete.
    public async Task RestoreAsync(Review review)
    {
        var rows = await _context.Reviews.IgnoreQueryFilters()
            .Where(r => r.Id == review.Id && r.IsDeleted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(r => r.IsDeleted, false)
                .SetProperty(r => r.DeletedAt, (DateTime?)null));

        if (rows == 0 && review.IsDeleted)
            throw new InvalidOperationException($"Không thể khôi phục đánh giá #{review.Id}.");

        review.IsDeleted = false;
        review.DeletedAt = null;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}