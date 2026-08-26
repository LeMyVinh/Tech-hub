namespace ECommerce.Application;

public interface IReviewService
{
    Task<ReviewResponse> CreateReviewAsync(int userId, CreateReviewRequest request);
    Task<ReviewListResponse> GetProductReviewsAsync(int productId, int page, int pageSize);
    Task<ReviewListResponse> GetPendingReviewsAsync(int page, int pageSize);

    // ADMIN: lấy toàn bộ đánh giá (mọi trạng thái, bao gồm cả đã xóa mềm) để hiển
    // thị ở trang quản trị, tương tự GetAllUsersAsync trong IUserService.
    Task<ReviewListResponse> GetAllReviewsAsync(int page, int pageSize);

    Task<ReviewResponse> ApproveReviewAsync(int reviewId);
    Task<ReviewResponse> RejectReviewAsync(int reviewId, string? reason);

    // SOFT DELETE
    Task<ReviewResponse> DeleteReviewAsync(int reviewId);
    Task<ReviewResponse> RestoreReviewAsync(int reviewId);
}