using ECommerce.Domain;

namespace ECommerce.Application;

public class ReviewService : IReviewService
{
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

        // Check if already reviewed
        var existingReview = await _reviewRepository.GetByOrderItemIdAsync(request.OrderItemId);
        if (existingReview is not null)
            throw new ReviewException(400, "Bạn đã đánh giá sản phẩm này rồi.");

        // Verify order item belongs to user and order is delivered
        var orderItem = await _orderRepository.GetByIdAsync(request.OrderItemId);
        if (orderItem is null)
            throw new ReviewException(404, "Đơn hàng không tồn tại.");

        // Create review
        var review = new Review
        {
            OrderItemId = request.OrderItemId,
            ProductId = request.ProductId,
            UserId = userId,
            Rating = (sbyte)request.Rating,
            Comment = request.Comment,
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
            review.CreatedAt
        );
    }
}
