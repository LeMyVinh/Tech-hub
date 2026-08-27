using ECommerce.Domain;

namespace ECommerce.Application;

public class ReviewService : IReviewService
{
    private const int MaxReviewImages = 5;
    private const int MaxCommentLength = 2000;

    private readonly IReviewRepository _reviewRepository;
    private readonly IOrderRepository _orderRepository;

    public ReviewService(IReviewRepository reviewRepository, IOrderRepository orderRepository)
    {
        _reviewRepository = reviewRepository;
        _orderRepository = orderRepository;
    }

    public async Task<ReviewResponse> CreateReviewAsync(int userId, CreateReviewRequest request)
    {
        if (request.Rating < 1 || request.Rating > 5)
            throw new ReviewException(400, "Đánh giá phải từ 1 đến 5 sao.");

        if (!string.IsNullOrEmpty(request.Comment) && request.Comment.Trim().Length > MaxCommentLength)
            throw new ReviewException(400, $"Nội dung đánh giá không được vượt quá {MaxCommentLength} ký tự.");

        if (request.ImageUrls != null && request.ImageUrls.Count > MaxReviewImages)
            throw new ReviewException(400, $"Chỉ được tải lên tối đa {MaxReviewImages} hình ảnh cho mỗi đánh giá.");

        // FIX (TC-09): GetByOrderItemIdAsync giờ dùng IgnoreQueryFilters() nên sẽ tìm thấy
        // review cũ NGAY CẢ KHI nó đã bị Admin xóa mềm trước đó. Vì Review.OrderItemId có
        // UNIQUE INDEX ở tầng DB (một sản phẩm trong đơn chỉ được review đúng 1 lần, kể cả
        // sau khi review đó bị xóa), business rule ở đây là: KHÔNG cho review lại một khi
        // đã review, dù review cũ có bị Admin ẩn đi. Điều này chặn từ tầng ứng dụng (400
        // thân thiện) thay vì để rơi xuống tầng DB (DbUpdateException -> 500 thô như trước).
        var existingReview = await _reviewRepository.GetByOrderItemIdAsync(request.OrderItemId);
        if (existingReview is not null)
        {
            var message = existingReview.IsDeleted
                ? "Bạn đã đánh giá sản phẩm này trước đây (đánh giá đã bị quản trị viên gỡ bỏ) nên không thể gửi đánh giá mới cho sản phẩm này."
                : "Bạn đã đánh giá sản phẩm này rồi.";
            throw new ReviewException(400, message);
        }

        var orderItem = await _orderRepository.GetOrderItemWithDetailsAsync(request.OrderItemId);
        if (orderItem is null)
            throw new ReviewException(404, "Đơn hàng không tồn tại.");

        if (orderItem.Order.UserId != userId)
            throw new ReviewException(403, "Bạn không có quyền đánh giá đơn hàng này.");

        if (orderItem.Order.Status != "Delivered")
            throw new ReviewException(400, "Chỉ có thể đánh giá sản phẩm sau khi đơn hàng đã được giao.");

        if (orderItem.ProductVariant.ProductId != request.ProductId)
            throw new ReviewException(400, "Sản phẩm không khớp với đơn hàng.");

        var review = new Review
        {
            OrderItemId = request.OrderItemId,
            ProductId = request.ProductId,
            UserId = userId,
            Rating = (sbyte)request.Rating,
            Comment = request.Comment?.Trim(),
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            ReviewImages = request.ImageUrls?.Select(url => new ReviewImage
            {
                ImageUrl = url
            }).ToList() ?? new List<ReviewImage>()
        };

        await _reviewRepository.AddAsync(review);
        await _reviewRepository.SaveChangesAsync();

        return MapToResponse(review);
    }

    public async Task<ReviewListResponse> GetProductReviewsAsync(int productId, int page, int pageSize)
    {
        var reviews = await _reviewRepository.GetByProductIdAsync(productId, page, pageSize);
        var totalCount = await _reviewRepository.GetByProductIdCountAsync(productId);
        var averageRating = await _reviewRepository.GetAverageRatingAsync(productId);

        return new ReviewListResponse(
            reviews.Select(MapToResponse).ToList(),
            totalCount,
            page,
            pageSize,
            averageRating
        );
    }

    public async Task<ReviewListResponse> GetPendingReviewsAsync(int page, int pageSize)
    {
        var reviews = await _reviewRepository.GetPendingReviewsAsync(page, pageSize);
        var totalCount = await _reviewRepository.GetPendingReviewsCountAsync();

        return new ReviewListResponse(
            reviews.Select(MapToResponse).ToList(),
            totalCount,
            page,
            pageSize,
            0
        );
    }

    public async Task<ReviewListResponse> GetAllReviewsAsync(int page, int pageSize)
    {
        var reviews = await _reviewRepository.GetAllReviewsAsync(page, pageSize);
        var totalCount = await _reviewRepository.GetAllReviewsCountAsync();

        return new ReviewListResponse(
            reviews.Select(MapToResponse).ToList(),
            totalCount,
            page,
            pageSize,
            0
        );
    }

    public async Task<ReviewResponse> ApproveReviewAsync(int reviewId)
    {
        var review = await _reviewRepository.GetByIdAsync(reviewId)
            ?? throw new ReviewException(404, "Đánh giá không tồn tại.");

        if (review.Status != "Pending")
            throw new ReviewException(400, "Đánh giá đã được xử lý.");

        review.Status = "Approved";
        await _reviewRepository.SaveChangesAsync();

        return MapToResponse(review);
    }

    public async Task<ReviewResponse> RejectReviewAsync(int reviewId, string? reason)
    {
        var review = await _reviewRepository.GetByIdAsync(reviewId)
            ?? throw new ReviewException(404, "Đánh giá không tồn tại.");

        if (review.Status != "Pending")
            throw new ReviewException(400, "Đánh giá đã được xử lý.");

        review.Status = "Rejected";
        review.RejectReason = reason;
        await _reviewRepository.SaveChangesAsync();

        return MapToResponse(review);
    }

    public async Task<ReviewResponse> DeleteReviewAsync(int reviewId)
    {
        var review = await _reviewRepository.GetByIdIncludingDeletedAsync(reviewId)
            ?? throw new ReviewException(404, "Đánh giá không tồn tại.");

        if (review.IsDeleted)
            throw new ReviewException(400, "Đánh giá này đã bị xóa trước đó.");

        await _reviewRepository.SoftDeleteAsync(review);

        return MapToResponse(review);
    }

    public async Task<ReviewResponse> RestoreReviewAsync(int reviewId)
    {
        var review = await _reviewRepository.GetByIdIncludingDeletedAsync(reviewId)
            ?? throw new ReviewException(404, "Đánh giá không tồn tại.");

        if (!review.IsDeleted)
            throw new ReviewException(400, "Đánh giá này chưa bị xóa.");

        await _reviewRepository.RestoreAsync(review);

        return MapToResponse(review);
    }

    private static ReviewResponse MapToResponse(Review review)
    {
        return new ReviewResponse(
            review.Id,
            review.ProductId,
            review.User.FullName,
            review.Rating,
            review.Comment,
            review.ReviewImages.Select(i => i.ImageUrl).ToList(),
            review.Status,
            review.RejectReason,
            review.CreatedAt,
            review.IsDeleted,
            review.DeletedAt
        );
    }
}