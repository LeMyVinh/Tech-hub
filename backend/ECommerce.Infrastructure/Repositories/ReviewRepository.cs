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
        return await _context.Reviews
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.OrderItemId == orderItemId);
    }

    public async Task<List<Review>> GetByProductIdAsync(int productId, int page, int pageSize)
    {
        return await _context.Reviews
            .IgnoreQueryFilters()
            .Include(r => r.User)
            .Include(r => r.ReviewImages)
            .Where(r => r.ProductId == productId && r.Status == "Approved" && !r.IsDeleted)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetByProductIdCountAsync(int productId)
    {
        return await _context.Reviews.CountAsync(r => r.ProductId == productId && r.Status == "Approved");
    }

    // FIX: cùng lý do như GetByProductIdAsync -- Admin phải luôn thấy đủ review Pending để
    // duyệt, kể cả khi sản phẩm hoặc tài khoản người viết review đã bị soft-delete sau đó.
    public async Task<List<Review>> GetPendingReviewsAsync(int page, int pageSize)
    {
        return await _context.Reviews
            .IgnoreQueryFilters()
            .Include(r => r.User)
            .Include(r => r.Product)
            .Include(r => r.ReviewImages)
            .Where(r => r.Status == "Pending" && !r.IsDeleted)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetPendingReviewsCountAsync()
    {
        return await _context.Reviews.CountAsync(r => r.Status == "Pending");
    }

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