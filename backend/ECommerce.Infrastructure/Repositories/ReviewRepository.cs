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

    public async Task<double> GetAverageRatingAsync(int productId)
    {
        var reviews = await _context.Reviews
            .Where(r => r.ProductId == productId && r.Status == "Approved")
            .ToListAsync();

        return reviews.Any() ? reviews.Average(r => r.Rating) : 0;
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
