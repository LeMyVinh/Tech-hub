using ECommerce.Domain;

namespace ECommerce.Application;

public interface IReviewRepository
{
    Task<Review?> GetByIdAsync(int id);
    Task<Review?> GetByOrderItemIdAsync(int orderItemId);
    Task<List<Review>> GetByProductIdAsync(int productId, int page, int pageSize);
    Task<int> GetByProductIdCountAsync(int productId);
    Task<List<Review>> GetPendingReviewsAsync(int page, int pageSize);
    Task<int> GetPendingReviewsCountAsync();
    Task<double> GetAverageRatingAsync(int productId);
    Task AddAsync(Review review);
    Task SaveChangesAsync();
}
