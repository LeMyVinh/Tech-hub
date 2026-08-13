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

    // PERF FIX: trước đây hàm này tải TOÀN BỘ các dòng Review (Approved) của sản
    // phẩm vào bộ nhớ ứng dụng (ToListAsync) chỉ để tính trung bình một cột Rating
    // bằng LINQ-to-Objects. Với sản phẩm có nhiều review, đây là lượng dữ liệu
    // không cần thiết phải kéo qua network + serialize. Giờ dùng AverageAsync để
    // SQL Server/MySQL tính AVG() trực tiếp, chỉ trả về một giá trị số duy nhất.
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

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}