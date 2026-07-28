namespace ECommerce.Application;

public interface IReviewService
{
    Task<ReviewResponse> CreateReviewAsync(int userId, CreateReviewRequest request);
    Task<ReviewListResponse> GetProductReviewsAsync(int productId, int page, int pageSize);
    Task<ReviewListResponse> GetPendingReviewsAsync(int page, int pageSize);
    Task<ReviewResponse> ApproveReviewAsync(int reviewId);
    Task<ReviewResponse> RejectReviewAsync(int reviewId, string? reason);
}
